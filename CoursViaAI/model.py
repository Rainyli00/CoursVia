import torch
import torch.nn as nn
from torch.nn import functional as F


class CausalSelfAttention(nn.Module):
    def __init__(self, config):
        super().__init__()

        assert config.N_EMBD % config.N_HEAD == 0

        self.c_attn = nn.Linear(config.N_EMBD, 3 * config.N_EMBD, bias=False)
        self.c_proj = nn.Linear(config.N_EMBD, config.N_EMBD, bias=False)

        self.attn_dropout_value = config.DROPOUT
        self.resid_dropout = nn.Dropout(config.DROPOUT)

        self.n_head = config.N_HEAD
        self.n_embd = config.N_EMBD

    def forward(self, x):
        b, t, c = x.size()

        q, k, v = self.c_attn(x).split(self.n_embd, dim=2)

        k = k.view(b, t, self.n_head, c // self.n_head).transpose(1, 2)
        q = q.view(b, t, self.n_head, c // self.n_head).transpose(1, 2)
        v = v.view(b, t, self.n_head, c // self.n_head).transpose(1, 2)

        y = F.scaled_dot_product_attention(
            q,
            k,
            v,
            attn_mask=None,
            dropout_p=self.attn_dropout_value if self.training else 0.0,
            is_causal=True,
        )

        y = y.transpose(1, 2).contiguous().view(b, t, c)
        y = self.resid_dropout(self.c_proj(y))

        return y


class Block(nn.Module):
    def __init__(self, config):
        super().__init__()

        self.ln_1 = nn.LayerNorm(config.N_EMBD)
        self.attn = CausalSelfAttention(config)
        self.ln_2 = nn.LayerNorm(config.N_EMBD)

        self.mlp = nn.ModuleDict(
            dict(
                c_fc=nn.Linear(config.N_EMBD, 4 * config.N_EMBD, bias=False),
                c_proj=nn.Linear(4 * config.N_EMBD, config.N_EMBD, bias=False),
                dropout=nn.Dropout(config.DROPOUT),
            )
        )

        self.gelu = nn.GELU()

    def forward(self, x):
        x = x + self.attn(self.ln_1(x))
        x = x + self.mlp.dropout(
            self.mlp.c_proj(
                self.gelu(
                    self.mlp.c_fc(self.ln_2(x))
                )
            )
        )

        return x


class MiniCoursViaLLM(nn.Module):
    def __init__(self, config):
        super().__init__()

        self.config = config

        self.transformer = nn.ModuleDict(
            dict(
                wte=nn.Embedding(config.VOCAB_SIZE, config.N_EMBD),
                wpe=nn.Embedding(config.BLOCK_SIZE, config.N_EMBD),
                drop=nn.Dropout(config.DROPOUT),
                h=nn.ModuleList([Block(config) for _ in range(config.N_LAYER)]),
                ln_f=nn.LayerNorm(config.N_EMBD),
            )
        )

        self.lm_head = nn.Linear(config.N_EMBD, config.VOCAB_SIZE, bias=False)

        # Weight tying
        self.transformer.wte.weight = self.lm_head.weight

        self.apply(self._init_weights)

    def _init_weights(self, module):
        if isinstance(module, nn.Linear):
            nn.init.normal_(module.weight, mean=0.0, std=0.02)

        elif isinstance(module, nn.Embedding):
            nn.init.normal_(module.weight, mean=0.0, std=0.02)

    def forward(self, idx, targets=None):
        device = idx.device
        _, t = idx.size()

        if t > self.config.BLOCK_SIZE:
            raise ValueError(
                f"Sequence length {t}, BLOCK_SIZE değerini aşıyor: {self.config.BLOCK_SIZE}"
            )

        pos = torch.arange(0, t, dtype=torch.long, device=device)

        tok_emb = self.transformer.wte(idx)
        pos_emb = self.transformer.wpe(pos)

        x = self.transformer.drop(tok_emb + pos_emb)

        for block in self.transformer.h:
            x = block(x)

        x = self.transformer.ln_f(x)

        logits = self.lm_head(x)

        loss = None

        if targets is not None:
            loss = F.cross_entropy(
                logits.reshape(-1, logits.size(-1)),
                targets.reshape(-1),
                ignore_index=-1,
            )

        return logits, loss

    @torch.no_grad()
    def generate(
        self,
        idx,
        max_new_tokens,
        temperature=0.0,
        top_k=0,
        repetition_penalty=1.0,
        eos_token_id=None,
    ):
        generated = idx

        for _ in range(max_new_tokens):
            idx_cond = generated[:, -self.config.BLOCK_SIZE:]

            logits, _ = self(idx_cond)
            logits = logits[:, -1, :]

            if repetition_penalty and repetition_penalty > 1.0:
                used_tokens = set(generated[0].tolist())

                for token_id in used_tokens:
                    if 0 <= token_id < logits.size(-1):
                        if logits[0, token_id] > 0:
                            logits[0, token_id] /= repetition_penalty
                        else:
                            logits[0, token_id] *= repetition_penalty

            if temperature is None or temperature <= 0:
                idx_next = torch.argmax(logits, dim=-1, keepdim=True)
            else:
                logits = logits / temperature

                if top_k and top_k > 0:
                    values, _ = torch.topk(logits, min(top_k, logits.size(-1)))
                    logits[logits < values[:, [-1]]] = -float("inf")

                probs = F.softmax(logits, dim=-1)
                idx_next = torch.multinomial(probs, num_samples=1)

            generated = torch.cat((generated, idx_next), dim=1)

            if eos_token_id is not None and int(idx_next.item()) == int(eos_token_id):
                break

        return generated