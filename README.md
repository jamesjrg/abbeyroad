#Abbey Road

At someone else's suggestion I made a web page that displays the web cam image from the camera at the Abbey Road crossing (of Beatles album cover fame) and plays piano notes when people walk across the bars of the crossing.

Currently (and very likely forever) it is just a proof of concept with some serious flaws:

1. the lag between detection and the piano note being played in the browser is far too big. It takes time to detect motion, send that result over the internet via websockets, and then wait for the Web Audio API to respond to Javascript. Delaying the video feed would be very difficult, because the source RTMP feed seems to be obfuscated somehow.

2. My simple motion detection code is terrible at telling the difference between pedestrians and cars, so given how busy the road is the end result is continuous terribad noise.

However, it does in some sense actually work:

- it creates an automated browser window on the server and then takes continuous screenshots of the webcam feed (as above I don't have access to the raw RTMP feed, it seems they have obfuscated it somehow)
- OpenCV is used on the server to detect movement within each polygonal segment of the crossing, discounting overly large movements as probably being a car
- The server serves a web page to clients which shows the webcam embedded in an iframe (the webcam won't allow embedding the flash directly)
- The server uses websockets to tell clients which keys are being pressed
- the client browser uses HTML 5 web audio to play sounds matching the "keys" being pressed
