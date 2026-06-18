from pathlib import Path

# Projenin kök klasörü. Diğer tüm yol ayarları buradan türetilir.
BASE_DIR = Path(__file__).resolve().parent

# Veri, model ve çıktı klasörleri.
DATA_DIR = BASE_DIR / "data"
PROCESSED_DATA_DIR = DATA_DIR / "processed"
MODELS_DIR = BASE_DIR / "models"
FINAL_MODEL_DIR = MODELS_DIR / "final"
OUTPUTS_DIR = BASE_DIR / "outputs"

TRAIN_FILE = PROCESSED_DATA_DIR / "coursvia_train.txt"
VAL_FILE = PROCESSED_DATA_DIR / "coursvia_val.txt"
TEST_FILE = PROCESSED_DATA_DIR / "coursvia_test.txt"

TOKENIZER_FILE = FINAL_MODEL_DIR / "coursvia_tokenizer.json"
FINAL_MODEL_FILE = FINAL_MODEL_DIR / "minicoursvia_llm_final.pt"
BEST_MODEL_FILE = FINAL_MODEL_DIR / "minicoursvia_llm_best.pt"

# Çalışma sırasında ihtiyaç duyulan klasörler yoksa otomatik oluşturulur.
for directory in [PROCESSED_DATA_DIR, FINAL_MODEL_DIR, OUTPUTS_DIR]:
    directory.mkdir(parents=True, exist_ok=True)

# ==========================================================
# MiniCoursViaLLM V3
# Amaç:
# Serbest sohbet modeli değil.
# Sadece CoursVia verilerine göre kontrollü öneri üreten küçük bir transformer tabanlı bir model.
# ==========================================================

VOCAB_SIZE = 12000
BLOCK_SIZE = 512

# RTX 3050 Laptop GPU için daha dengeli ayar
# Küçük GPU belleği için gerçek batch küçük tutulur, gradient accumulation ile etkili batch büyütülür.
BATCH_SIZE = 4
GRAD_ACCUM_STEPS = 8

# Eğitim ve validasyon döngüsü ayarları.
MAX_ITERS = 4000
EVAL_INTERVAL = 100
EVAL_ITERS = 40


N_EMBD = 512
N_HEAD = 8
N_LAYER = 8

DROPOUT = 0.10
LEARNING_RATE = 2.5e-4
WEIGHT_DECAY = 0.10
GRAD_CLIP = 1.0

# Üretim tarafında kullanılan varsayılan decoding ayarları.
GENERATE_MAX_NEW_TOKENS = 650
TEMPERATURE = 0.0
TOP_K = 0
REPETITION_PENALTY = 1.05

RANDOM_SEED = 42
