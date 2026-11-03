"""웹게임에서 뽑은 그림과 맵 데이터를 받아 Unity 폴더에 저장하는 작은 로컬 서버.

웹게임은 브라우저 Canvas로 맵을 그리므로, 브라우저 안에서 그린 결과를 그대로 받아 오는 것이 가장 정확하다.
브라우저(웹게임 개발 서버 페이지)가 `export_webgame.js`를 이 서버에서 불러와 실행하고, 결과를 POST /save로 보낸다.

- GET  /export_webgame.js : 내보내기 모듈(같은 폴더의 파일)
- POST /save?path=<상대 경로> : 본문을 <root>/<상대 경로>에 쓴다. root 밖으로 나가는 경로는 거절한다.

사용: python -I export_server.py <root> [--port 4330]
"""
import argparse
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import parse_qs, urlparse

HERE = Path(__file__).resolve().parent


def make_handler(root: Path):
    class Handler(BaseHTTPRequestHandler):
        def cors(self):
            self.send_header("Access-Control-Allow-Origin", "*")
            self.send_header("Access-Control-Allow-Methods", "GET, POST, OPTIONS")
            self.send_header("Access-Control-Allow-Headers", "Content-Type")

        def do_OPTIONS(self):
            self.send_response(204)
            self.cors()
            self.end_headers()

        def do_GET(self):
            if urlparse(self.path).path != "/export_webgame.js":
                self.send_error(404)
                return
            body = (HERE / "export_webgame.js").read_bytes()
            self.send_response(200)
            self.cors()
            self.send_header("Content-Type", "text/javascript; charset=utf-8")
            self.send_header("Cache-Control", "no-store")
            self.end_headers()
            self.wfile.write(body)

        def do_POST(self):
            url = urlparse(self.path)
            relative = parse_qs(url.query).get("path", [""])[0]
            target = (root / relative).resolve()
            if url.path != "/save" or not relative or root not in target.parents:
                self.send_error(400, "bad path")
                return
            body = self.rfile.read(int(self.headers.get("Content-Length", 0)))
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(body)
            self.send_response(200)
            self.cors()
            self.end_headers()
            self.wfile.write(b"ok")
            print(f"saved {relative} ({len(body)} bytes)", flush=True)

        def log_message(self, *args):
            pass

    return Handler


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("root")
    parser.add_argument("--port", type=int, default=4330)
    args = parser.parse_args()
    root = Path(args.root).resolve()
    root.mkdir(parents=True, exist_ok=True)
    print(f"export server on http://127.0.0.1:{args.port} -> {root}", flush=True)
    ThreadingHTTPServer(("127.0.0.1", args.port), make_handler(root)).serve_forever()


if __name__ == "__main__":
    main()
