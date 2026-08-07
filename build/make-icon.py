"""Draws assets/icon.png, the package icon: a lighthouse on a coral disc.

The design is "Maison lumineuse" by VectorPortal, from Flaticon, redrawn here:
    https://www.flaticon.com/fr/icones-gratuites/maison-lumineuse
Flaticon's free licence requires the credit, which the README carries. Keep it
there if you change this file, and replace it if you replace the design.

Kept in the repository so the icon stays editable. A PNG alone is a dead end --
nudging a colour or a proportion means redrawing the whole thing from scratch,
which is exactly how this file came to exist.

    python build/make-icon.py && mv icon-lighthouse.png assets/icon.png

Needs Pillow. Everything is drawn at 4x and downsampled, which is where the
antialiasing comes from.
"""

from PIL import Image, ImageDraw

K = 4                      # supersampling
S = 512 * K

RED    = (224, 93, 93)
NAVY   = (43, 46, 92)
YEL    = (253, 217, 126)
YEL_D  = (246, 199, 97)
DOME   = (167, 197, 221)
DOME_L = (205, 226, 240)
PERI   = (179, 192, 232)
LAV    = (233, 235, 247)
W      = 9 * K             # stroke width

def s(*p):
    return [(x * K, y * K) for x, y in p]

img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# Disc
d.ellipse([0, 0, S - 1, S - 1], fill=RED)

# Light rays
for a, b in [((132, 124), (198, 140)), ((136, 176), (197, 161)),
             ((380, 124), (314, 140)), ((376, 176), (315, 161))]:
    d.line(s(a, b), fill=NAVY, width=W, joint="curve")
    for p in (a, b):
        d.ellipse([p[0] * K - W // 2, p[1] * K - W // 2,
                   p[0] * K + W // 2, p[1] * K + W // 2], fill=NAVY)

# Tower: navy body, yellow inset
d.polygon(s((216, 190), (296, 190), (326, 426), (186, 426)), fill=NAVY)
d.polygon(s((225, 199), (287, 199), (313, 417), (199, 417)), fill=YEL)
d.polygon(s((272, 199), (287, 199), (313, 417), (288, 417)), fill=YEL_D)

# Stair notches down the shaded side
for i in range(9):
    t  = i / 9.0
    y  = 232 + t * 168
    x  = 283 + t * 24
    d.line(s((x - 13, y), (x, y)), fill=NAVY, width=4 * K)

# Balcony
d.rounded_rectangle([208 * K, 170 * K, 304 * K, 198 * K], radius=7 * K, fill=NAVY)
d.rounded_rectangle([(208 + 9) * K, (170 + 9) * K, (304 - 9) * K, (198 - 9) * K],
                    radius=3 * K, fill=PERI)

# Lantern room: navy arch, dome inset
d.rounded_rectangle([222 * K, 100 * K, 290 * K, 176 * K], radius=34 * K, fill=NAVY)
d.rounded_rectangle([231 * K, 109 * K, 281 * K, 176 * K], radius=25 * K, fill=DOME)
d.pieslice([231 * K, 109 * K, 281 * K, 159 * K], 185, 300, fill=DOME_L)

# Finial: a small cap on the apex, not a lollipop
d.ellipse([248 * K, 90 * K, 264 * K, 106 * K], fill=NAVY)

# Windows
def window(cx, top, w, h):
    d.rounded_rectangle([(cx - w) * K, top * K, (cx + w) * K, (top + h) * K],
                        radius=w * K, fill=NAVY)
    d.rounded_rectangle([(cx - w + 5) * K, (top + 5) * K, (cx + w - 5) * K, (top + h - 3) * K],
                        radius=(w - 5) * K, fill=LAV)

window(256, 142, 12, 38)     # the light itself
window(256, 226, 13, 34)
window(253, 274, 13, 34)
window(256, 322, 13, 34)

# Door
d.rounded_rectangle([236 * K, 372 * K, 278 * K, 420 * K], radius=21 * K, fill=NAVY)
d.rectangle([236 * K, 400 * K, 278 * K, 420 * K], fill=NAVY)
d.rounded_rectangle([245 * K, 381 * K, 269 * K, 420 * K], radius=12 * K, fill=LAV)
d.rectangle([245 * K, 402 * K, 269 * K, 420 * K], fill=LAV)

img.resize((512, 512), Image.LANCZOS).save("icon-lighthouse.png")
print("ok")
