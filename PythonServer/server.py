import socket
import json
import base64
import numpy as np
import cv2
import time
import os

TEMPLATE_FOLDER = "templates"

# 确保模板文件夹存在
if not os.path.exists(TEMPLATE_FOLDER):
    os.makedirs(TEMPLATE_FOLDER)
    print(f"📁 已创建模板文件夹: {TEMPLATE_FOLDER}")

def load_or_create_template(product_code, img):
    """根据产品编号加载模板，如果不存在则创建"""
    template_path = os.path.join(TEMPLATE_FOLDER, f"{product_code}.jpg")
    
    # 1. 尝试从硬盘加载
    if os.path.exists(template_path):
        template = cv2.imread(template_path, cv2.IMREAD_GRAYSCALE)
        if template is not None:
            print(f"📂 加载模板: {template_path}")
            return template
        else:
            print(f"❌ 模板文件损坏: {template_path}")
    
    # 2. 模板不存在或损坏，用当前图片创建
    if img is None:
        print("❌ 无法创建模板：图片无效")
        return None
    
    gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    cv2.imwrite(template_path, gray)
    print(f"📸 创建新模板: {template_path}")
    return gray

def detect_defect(img, product_code, threshold):
    """
    检测缺陷，返回 (result, confidence, defect_type, location)
    """
    # 获取模板（每次重新加载，确保不会用错）
    template = load_or_create_template(product_code, img)
    if template is None:
        return "NG", 0, "模板未就绪", None

    # 1. 转为灰度并调整尺寸（以模板为准）
    h, w = template.shape
    gray_img = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    if gray_img.shape[0] != h or gray_img.shape[1] != w:
        gray_img = cv2.resize(gray_img, (w, h))

    # 2. 高斯模糊去噪
    gray_img = cv2.GaussianBlur(gray_img, (5, 5), 0)
    template_blur = cv2.GaussianBlur(template, (5, 5), 0)

    # 3. 差分
    diff = cv2.absdiff(gray_img, template_blur)

    # 4. 二值化
    _, thresh = cv2.threshold(diff, 30, 255, cv2.THRESH_BINARY)

    # 5. 形态学去噪
    kernel = np.ones((5, 5), np.uint8)
    thresh = cv2.morphologyEx(thresh, cv2.MORPH_OPEN, kernel, iterations=1)

    # 6. 计算差异面积占比
    diff_area = np.sum(thresh > 0)
    total_area = thresh.size
    diff_ratio = diff_area / total_area
    print(f"🔍 差异面积占比: {diff_ratio:.4%} (阈值: {threshold:.0%})")

    # 7. 判定（使用传入的阈值）
    if diff_ratio > threshold:
        result = "NG"
        confidence = 1 - diff_ratio
        contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        location = None
        if contours:
            max_contour = max(contours, key=cv2.contourArea)
            x, y, w_box, h_box = cv2.boundingRect(max_contour)
            location = {"x": x, "y": y, "w": w_box, "h": h_box}
        if diff_ratio > 0.3:
            defect_type = "严重缺陷"
        else:
            defect_type = "轻微缺陷"
    else:
        result = "OK"
        confidence = 1 - diff_ratio
        defect_type = "无"
        location = None

    return result, confidence, defect_type, location

def handle_client(conn, addr):
    print(f"✅ 连接成功！客户端地址: {addr}")
    rfile = conn.makefile('r', encoding='utf-8')
    wfile = conn.makefile('w', encoding='utf-8')

    while True:
        try:
            line = rfile.readline()
            if not line:
                print("⚠️ 客户端已断开连接")
                break
            line = line.strip()
            if not line:
                continue
            print(f"📩 收到 C# 发来的消息: {line[:100]}...")

            try:
                json_obj = json.loads(line)
            except json.JSONDecodeError as e:
                print(f"❌ JSON解析失败: {e}")
                continue

            cmd = json_obj.get("cmd", "")
            if cmd == "ping":
                response = {"status": "pong"}
                wfile.write(json.dumps(response) + "\n")
                wfile.flush()
                print("💓 回复心跳 pong")
                continue

            elif cmd == "detect":
                img_base64 = json_obj.get("image_base64", "")
                product_code = json_obj.get("product_code", "Unknown")
                threshold = json_obj.get("threshold", 0.05)
                print(f"📊 产品: {product_code}, 阈值: {threshold}")

                # 解码图片
                try:
                    img_data = base64.b64decode(img_base64)
                    np_arr = np.frombuffer(img_data, np.uint8)
                    img = cv2.imdecode(np_arr, cv2.IMREAD_COLOR)
                except Exception as e:
                    print(f"❌ 图片解码异常: {e}")
                    result = {
                        "status": "error",
                        "result": "NG",
                        "confidence": 0,
                        "defect_type": "解码异常",
                        "location": None,
                        "timestamp": time.strftime("%Y-%m-%d %H:%M:%S")
                    }
                    wfile.write(json.dumps(result) + "\n")
                    wfile.flush()
                    continue

                if img is None:
                    print("❌ 图片解码失败，可能数据损坏")
                    result = {
                        "status": "error",
                        "result": "NG",
                        "confidence": 0,
                        "defect_type": "图片无效",
                        "location": None,
                        "timestamp": time.strftime("%Y-%m-%d %H:%M:%S")
                    }
                    wfile.write(json.dumps(result) + "\n")
                    wfile.flush()
                    continue

                print(f"🔍 收到图片，尺寸: {img.shape}，产品编号: {product_code}")

                # ---- 调用缺陷检测，传入产品编号和阈值 ----
                result_str, confidence, defect_type, location = detect_defect(img, product_code, threshold)

                # 构造返回结果
                result = {
                    "status": "success",
                    "result": result_str,
                    "confidence": round(confidence, 4),
                    "defect_type": defect_type,
                    "location": location,
                    "timestamp": time.strftime("%Y-%m-%d %H:%M:%S")
                }
                wfile.write(json.dumps(result) + "\n")
                wfile.flush()
                print(f"✅ 已返回检测结果: {result_str}")

            else:
                print(f"⚠️ 未知命令: {cmd}")

        except Exception as e:
            print(f"❌ 处理消息时异常: {e}")
            break

    rfile.close()
    wfile.close()
    conn.close()
    print("🛑 连接已关闭")

def main():
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server_socket.bind(('0.0.0.0', 8888))
    server_socket.listen(1)
    print("⭐ Python 视觉服务已启动，等待 C# 客户端连接...")
    print("📡 监听端口: 8888")

    conn, addr = server_socket.accept()
    handle_client(conn, addr)

    server_socket.close()
    print("🛑 Python 服务已关闭")

if __name__ == "__main__":
    main()