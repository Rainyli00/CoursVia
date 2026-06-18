import torch
import torch.nn as nn
from torch.nn import functional as F


# GPT tarzı nedensel self-attention katmanı.
# Her token sadece kendisini ve önceki tokenları görebilir; bu yüzden üretimde gelecek token sızıntısı olmaz.
class CausalSelfAttention(nn.Module):
    def __init__(self, config):
        super().__init__()

        # Embedding boyutu head sayısına tam bölünmeli ki her head eşit kanal alabilsin.
        assert config.N_EMBD % config.N_HEAD == 0

        # Tek linear katmanda query, key ve value birlikte üretilir; sonra 3 parçaya ayrılır.
        self.c_attn = nn.Linear(config.N_EMBD, 3 * config.N_EMBD, bias=False)

        # Attention sonrası tüm head'lerden gelen bilgi tekrar embedding boyutuna projekte edilir.
        self.c_proj = nn.Linear(config.N_EMBD, config.N_EMBD, bias=False)

        self.attn_dropout_value = config.DROPOUT
        self.resid_dropout = nn.Dropout(config.DROPOUT)

        self.n_head = config.N_HEAD
        self.n_embd = config.N_EMBD

    def forward(self, x):
        # x şekli: batch, sequence_length, embedding_dim
        b, t, c = x.size()

        # q, k, v aynı lineardan çıkar ve embedding boyutu kadar üç parçaya ayrılır.
        q, k, v = self.c_attn(x).split(self.n_embd, dim=2)

        # Head boyutu: embedding_dim / head_count.
        # Transpose sonrası şekil: batch, head, sequence_length, head_dim
        k = k.view(b, t, self.n_head, c // self.n_head).transpose(1, 2)
        q = q.view(b, t, self.n_head, c // self.n_head).transpose(1, 2)
        v = v.view(b, t, self.n_head, c // self.n_head).transpose(1, 2)

        # PyTorch'un optimize attention fonksiyonu kullanılır.
        # is_causal=True olduğu için tokenlar gelecek pozisyonlara bakamaz.
        y = F.scaled_dot_product_attention(
            q,
            k,
            v,
            attn_mask=None,
            dropout_p=self.attn_dropout_value if self.training else 0.0,
            is_causal=True,
        )

        # Head'ler tekrar birleştirilir ve residual yola girmeden önce son projeksiyon/dropout uygulanır.
        y = y.transpose(1, 2).contiguous().view(b, t, c)
        y = self.resid_dropout(self.c_proj(y))

        return y


# Transformer bloğu: pre-norm attention + pre-norm MLP.
# Residual bağlantılar modelin daha derin katmanlarda stabil öğrenmesine yardım eder.
class Block(nn.Module):
    def __init__(self, config):
        super().__init__()

        # Attention öncesi LayerNorm.
        self.ln_1 = nn.LayerNorm(config.N_EMBD)
        self.attn = CausalSelfAttention(config)

        # MLP öncesi LayerNorm.
        self.ln_2 = nn.LayerNorm(config.N_EMBD)

        # Feed-forward ağında GPT geleneğiyle embedding boyutu 4 katına çıkarılıp geri düşürülür.
        self.mlp = nn.ModuleDict(
            dict(
                c_fc=nn.Linear(config.N_EMBD, 4 * config.N_EMBD, bias=False),
                c_proj=nn.Linear(4 * config.N_EMBD, config.N_EMBD, bias=False),
                dropout=nn.Dropout(config.DROPOUT),
            )
        )

        self.gelu = nn.GELU()

    def forward(self, x):
        # Pre-norm attention bloğu residual olarak ana akışa eklenir.
        x = x + self.attn(self.ln_1(x))

        # Pre-norm MLP bloğu da residual olarak eklenir.
        x = x + self.mlp.dropout(
            self.mlp.c_proj(
                self.gelu(
                    self.mlp.c_fc(self.ln_2(x))
                )
            )
        )

        return x


# MiniCoursVia için GPT benzeri küçük dil modeli.
# Token embedding + pozisyon embedding + N adet transformer block + language modeling head kullanır.
class MiniCoursViaLLM(nn.Module):
    def __init__(self, config):
        super().__init__()

        self.config = config

        # Transformer gövdesi ModuleDict içinde gruplanır.
        self.transformer = nn.ModuleDict(
            dict(
                # Token id -> embedding vektörü.
                wte=nn.Embedding(config.VOCAB_SIZE, config.N_EMBD),

                # Pozisyon id -> pozisyon embedding vektörü.
                wpe=nn.Embedding(config.BLOCK_SIZE, config.N_EMBD),
                drop=nn.Dropout(config.DROPOUT),

                # Ardışık transformer blokları.
                h=nn.ModuleList([Block(config) for _ in range(config.N_LAYER)]),

                # Son layer normalization.
                ln_f=nn.LayerNorm(config.N_EMBD),
            )
        )

        # Her pozisyon için vocab boyutunda next-token logit üretir.
        self.lm_head = nn.Linear(config.N_EMBD, config.VOCAB_SIZE, bias=False)

        # Weight tying: input token embedding ile output projection aynı ağırlığı paylaşır.
        # Bu hem parametre sayısını azaltır hem de dil modeli pratiğinde genelde daha iyi sonuç verir.
        self.transformer.wte.weight = self.lm_head.weight

        # Linear ve embedding katmanları küçük normal dağılımla başlatılır.
        self.apply(self._init_weights)

    def _init_weights(self, module):
        # GPT tarzı basit başlangıç: Linear ve Embedding ağırlıkları N(0, 0.02).
        if isinstance(module, nn.Linear):
            nn.init.normal_(module.weight, mean=0.0, std=0.02)

        elif isinstance(module, nn.Embedding):
            nn.init.normal_(module.weight, mean=0.0, std=0.02)

    def forward(self, idx, targets=None):
        device = idx.device
        _, t = idx.size()

        # Model en fazla BLOCK_SIZE kadar context görebilir.
        # Daha uzun sequence gelirse pozisyon embedding sınırı aşılır.
        if t > self.config.BLOCK_SIZE:
            raise ValueError(
                f"Sequence length {t}, BLOCK_SIZE değerini aşıyor: {self.config.BLOCK_SIZE}"
            )

        # 0..t-1 pozisyon id'leri her batch için ortak kullanılır.
        pos = torch.arange(0, t, dtype=torch.long, device=device)

        # Token ve pozisyon embeddingleri toplanarak transformer giriş temsili oluşturulur.
        tok_emb = self.transformer.wte(idx)
        pos_emb = self.transformer.wpe(pos)

        x = self.transformer.drop(tok_emb + pos_emb)

        # Tüm transformer blokları sırayla çalıştırılır.
        for block in self.transformer.h:
            x = block(x)

        x = self.transformer.ln_f(x)

        # Her token pozisyonu için vocab üzerindeki skorlar.
        logits = self.lm_head(x)

        loss = None

        # Eğitim sırasında targets verilirse next-token cross entropy loss hesaplanır.
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

        # Autoregressive üretim: her turda son token tahmin edilir ve girişe eklenir.
        for _ in range(max_new_tokens):
            # Context BLOCK_SIZE ile sınırlanır; model daha uzun geçmişi doğrudan göremez.
            idx_cond = generated[:, -self.config.BLOCK_SIZE:]

            # Sadece son pozisyonun logits'i bir sonraki token seçimi için kullanılır.
            logits, _ = self(idx_cond)
            logits = logits[:, -1, :]

            # Tekrar cezası daha önce üretilen tokenların tekrar seçilme ihtimalini azaltır.
            if repetition_penalty and repetition_penalty > 1.0:
                used_tokens = set(generated[0].tolist())

                for token_id in used_tokens:
                    if 0 <= token_id < logits.size(-1):
                        if logits[0, token_id] > 0:
                            logits[0, token_id] /= repetition_penalty
                        else:
                            logits[0, token_id] *= repetition_penalty

            # temperature <= 0 ise deterministik greedy decoding yapılır.
            if temperature is None or temperature <= 0:
                idx_next = torch.argmax(logits, dim=-1, keepdim=True)
            else:
                # Temperature büyüdükçe dağılım yumuşar ve daha çeşitli tokenlar seçilebilir.
                logits = logits / temperature

                # top_k > 0 ise sadece en olası k token sampling havuzunda kalır.
                if top_k and top_k > 0:
                    values, _ = torch.topk(logits, min(top_k, logits.size(-1)))
                    logits[logits < values[:, [-1]]] = -float("inf")

                probs = F.softmax(logits, dim=-1)
                idx_next = torch.multinomial(probs, num_samples=1)

            # Seçilen token dizinin sonuna eklenir.
            generated = torch.cat((generated, idx_next), dim=1)

            # End-of-text token üretilirse erken durulur.
            if eos_token_id is not None and int(idx_next.item()) == int(eos_token_id):
                break

        return generated
