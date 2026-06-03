"""Smoke test the running chat_server.py via HTTP."""
import json
import httpx

URL = "http://127.0.0.1:8765"

print("==== /healthz ====")
print(httpx.get(URL + "/healthz", timeout=5).json())
print()

CASES = [
    ("Sân vận động ở đâu", None),
    ("Nhà ăn ở đâu", None),
    ("Lớp học ở đâu", None),
    ("Ký túc xá ở đâu", None),
    ("Khu tự do ở đâu", None),
    ("Mấy giờ ăn cơm", None),
    ("Mấy giờ tập thể dục", None),
    ("Hôm nay học môn gì", {"currentDay": 5}),
    ("Hôm nay học môn gì", {"currentDay": 10}),
    ("Đại đội trưởng là ai", None),
    ("ddt la ai v", None),
    ("svd đâu v", None),
    ("Where is canteen", None),
    ("Today học gì", {"currentDay": 1}),
    ("san van dong o dau", None),
    ("Em chào thủ trưởng", None),
    ("Em xin phép", None),
    ("Cảm ơn anh", None),
    # original bug scenario
    ("khu A1 ở đâu", None),
    ("khu A2 ở đâu", None),
    ("khu A3 ở đâu", None),
    ("khu B ở đâu", None),
    # OOS
    ("Crypto giảm sốc", None),
    ("iPhone giá", None),
    ("Anh có người yêu chưa", None),
]

print("==== /chat ====")
print()
with httpx.Client(timeout=30) as cli:
    for q, gs in CASES:
        body = {"query": q, "useGroq": False, "gameState": gs}
        r = cli.post(URL + "/chat", json=body)
        d = r.json()
        route = d["route"]
        marker = {"template": "[OK]   ", "groq_rag": "[RAG]  ", "fallback": "[WEAK] "}[route]
        print(f"{marker} score={d['score']:.2f} {d['topIntent']:18s} {d['topEntity']:15s} | Q: {q}")
        # print first 100 chars of answer
        ans = d["answer"].replace("\n", " | ")
        print(f"        A: {ans[:160]}")
        print()
