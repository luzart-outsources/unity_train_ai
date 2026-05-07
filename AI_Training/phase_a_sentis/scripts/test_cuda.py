import torch

print(f"PyTorch version : {torch.__version__}")
print(f"CUDA available : {torch.cuda.is_available()}")
if torch.cuda.is_available():
    print(f"CUDA version : {torch.version.cuda}")
    print(f"GPU count : {torch.cuda.device_count()}")
    print(f"GPU name : {torch.cuda.get_device_name(0)}")
    print(f"Compute cap : {torch.cuda.get_device_capability(0)}")
    # Quick smoke test
    x = torch.randn(1000, 1000, device="cuda")
    y = x @ x
    print(f"GPU smoke test : OK (matmul {y.shape})")
else:
    print("WARNING: CUDA not available — training will fall back to CPU (slow).")
