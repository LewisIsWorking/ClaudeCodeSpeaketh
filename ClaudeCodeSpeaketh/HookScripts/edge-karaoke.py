# edge-karaoke.py -- synthesize with edge-tts AND emit per-word timings, so the
# resident app can highlight each word in time with the audio.
#
# Usage: python edge-karaoke.py <voice> <textfile> <out.mp3> <out.json> [rate]
# Writes the mp3 and a JSON array of {"o": offsetMs, "d": durationMs, "t": word}.
import sys
import json
import asyncio
import edge_tts


async def main():
    voice = sys.argv[1]
    text_file = sys.argv[2]
    mp3_path = sys.argv[3]
    json_path = sys.argv[4]
    rate = sys.argv[5] if len(sys.argv) > 5 else "+0%"

    with open(text_file, encoding="utf-8") as fh:
        text = fh.read()

    comm = edge_tts.Communicate(text, voice, rate=rate, boundary="WordBoundary")
    words = []
    with open(mp3_path, "wb") as audio:
        async for chunk in comm.stream():
            if chunk["type"] == "audio":
                audio.write(chunk["data"])
            elif chunk["type"] == "WordBoundary":
                words.append({
                    "o": chunk["offset"] // 10000,
                    "d": chunk["duration"] // 10000,
                    "t": chunk["text"],
                })

    with open(json_path, "w", encoding="utf-8") as fh:
        json.dump(words, fh)


if __name__ == "__main__":
    asyncio.run(main())
