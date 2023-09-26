import zmq
import numpy as np
from tensorflow.keras.models import load_model
from tensorflow.keras.preprocessing.image import load_img, img_to_array

model = load_model('horse_or_human_95%_accuracy.h5')

context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5555")

bytestosend = []

while True:
    bytes_received = socket.recv(3136)
    array_received = np.frombuffer(bytes_received, dtype=np.float32).reshape(9,9)

    pathstr = ""

    for i in range(9):
        for j in range(9):
            pathstr += chr(array_received[i][j])
    
    print(pathstr)
    # Load and prepare images
    img = load_img(pathstr, target_size=(300, 300))
    x = img_to_array(img)
    x /= 255
    x = np.expand_dims(x, axis=0)

    # images = np.vstack([x])
    # classes = model.predict(images, batch_size=10)
    classes = model.predict(x)
    
    if classes[0] > 0.5:
        pred = " is a human"
        socket.send(b"human")
    else:
        pred = " is a horse"
        socket.send(b"horse")

