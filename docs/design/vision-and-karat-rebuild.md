# Vision + Karat Pipeline — Rebuild Design (GK-VISION)

Date: 2026-07-03 · Status: Binding design note (owner asked Claude to architect this).
Source: analysis of the legacy GoldCube vision + karat code and the real sample frames
`recievedObjectTray → BW → Canny → Blobs` from transaction `C:\16-05-47`.

## 1. What the legacy actually does (the honest picture)

Live vision is one file: `GoldCube.BusinessLogic/Common/GCImageProcessingHelperNew.cs`,
`ProcessImage()`. Library: **Emgu.CV 2.4.0.1717** (OpenCV 2.4-era, net452 — unsupportable
on net10). Pipeline:

1. Load `recievedObjectTray.bmp` → greyscale.
2. `InRange` "BW" (red ≤ 200) — the blown-out coin clips to black, tray to white. This BW is
   effectively **dead** downstream (only feeds a discarded circle finder).
3. **Tray circle is hardcoded**: `center=(615,389)`, `radius=450` — NOT detected per frame.
   The chamber is shot at an angle so it is really an **ellipse**; a fixed circle is
   geometrically wrong and any camera bump silently misregisters every XRF move.
4. Circular ROI mask at 0.7·r; the visible red ring is drawn at 0.74·r.
5. Gaussian blur → **Canny(200,50)** → erase the ROI boundary ring → threshold. Only the
   coin outline + relief arcs survive (the smooth tray yields no edges).
6. Collect Canny edge pixels → **hand-rolled k-means (5 clusters, 10 iters)** → snap each
   centroid to the nearest edge pixel = the 5 XRF sample points (the coloured crosses).
   No dedup, no min-separation, no "is this pixel solid metal?" gate — points land on
   **edges** (rims, stone borders, holes), not flat metal.
7. Pixel→arm mm: `(X-615)·0.08747`, `(389-Y)·0.08747`; open-loop relative Dobot moves, no
   closed-loop verification the head reached metal.

There is **no true item-vs-tray segmentation and no multi-item guard** in the live path
(the old AbsDiff single-object check is commented out). "Item present" = "≥1 edge pixel".

Karat: `GCKaratCalculator` computes two values — density-weighted and raw-XRF-% — and a
config flag (`UseKaratUsingXrfPercentageForOffer=true`) picks the XRF-% one for the offer.
Element rejection thresholds, karat/silver floors, and a 65–89 volume correction band are
all present (full table + formulas preserved in the rebuild). The physical volume chamber
is currently disabled — "measured volume" is an AI pseudo-value, so the fraud cross-check
is presently comparing against an estimate, not a measurement.

## 2. Rebuild — GoldKiosk.Kiosk.Vision (net10.0-windows)

CV engine: **OpenCvSharp4** (+ `OpenCvSharp4.runtime.win`) — modern OpenCV 4.x, permissive
license (Emgu 4.x is GPL/commercial-dual — avoid the licence cost; ImageSharp has no
Canny/contours/ellipse/kmeans/warp — encode only).

Ports (pure library, no WinForms):
```
ITrayRectifier   Rectify(Mat) -> undistort + FitEllipse + homography → metric top-down ROI,
                 center, mm/px, confidence   (replaces the (615,389)/r450 hardcode)
IItemSegmenter   Segment(ROI) -> metal / stone / background masks + component count
                 (the multi-item / no-item guard the live path lost)
IXrfPointSelector Select(masks, maxPoints) -> interior on-metal points via distanceTransform
                 + farthest-point sampling + dedup + confidence (opposite of edge-snap)
IStoneEstimator  EstimateStones(masks, calibration, declaredType) -> per-stone area→carats→grams
IPixelToArmMapper ToArmMm(px, rectify) -> uses the fitted centre + metric scale
IVisionPipeline  Analyze(frame) -> sample points + stone weight + masks + debug overlays
                 (writes BW/Canny/Blobs-equivalent debug images via the legacy path contract
                 so MetalAnalyserCommandExecutor swaps in with minimal change)
```

Karat math stays in **GoldKiosk.Domain** (pure, unit-testable, no CV dependency): port
`GCKaratCalculator`, `GCMetalAnalysisPayload` thresholds, `GCVolumeRangeValidator`,
`GCGoldKaratMapper` bands, `ElementMap.txt` loader — and add the new input
`sellableMetalWeight = scaleWeight − stoneWeightGrams` to the karat/offer step.

## 3. The stone-weight feature (owner's idea, made concrete)

Within the metal-item hull, classify each pixel metal (high-L, warm gold hue) vs stone/gap
(dark shadow, saturated colour, or specular highlight not matching gold) using LAB
thresholds + GrabCut. `connectedComponentsWithStats` → per-stone pixel area → mm² via the
rectified metric scale. Stones are 3-D, so mass is modelled as a power law fit from
calibration: `m_stone = α · A_mm² ^ β` (β≈1.5 for roughly isometric cuts), per stone type
(ρ and cut factor differ: diamond 3.52, CZ 5.7, ruby/sapphire 4.0, glass 2.5; carats→grams
×0.2). `sellable_metal = scale_weight − Σ m_stone` (clamped ≥0, ≤ scale weight). High/ambiguous
stone coverage → route to live-agent review, never auto-offer.

**Calibration data required (the R&D deliverable):** a reference set of jewellery with known
stone counts/types and independently measured carat weights, imaged in the rectified rig;
regress carats on measured mm² to fit `(α,β)` per type; store as `stone-calibration.json`
alongside `ElementMap.txt`. Bias conservative when uncertain (never overpay). Until
calibrated, the estimator runs in report-only mode (surfaces an estimate, does not adjust
the paid weight).

## 4. Deterministic ports vs R&D

Deterministic (re-implement as-is): all karat math + thresholds + volume band + density
table + config keys (MillimeterPerPixel 0.08747, MaxBlobsToScan 5, ElementPercentageToReject 2,
MinGoldKarat 9.2, MinSilver% 80, GramPerPennyWeight 0.64301, FraudDetection true,
UseKaratUsingXrfPercentageForOffer true), the XRF scan/merge loop (with a robust/weighted
merge instead of plain average), the pixel→mm affine.

R&D / calibration: camera intrinsic calibration + undistort; ellipse fit + homography
rectification; exposure fix so metal isn't clipped (prerequisite for all colour work);
metal/stone/background segmentation tuning; on-metal point selector tuning; the stone-weight
model; re-basing the volume cross-check on a real signal.

## 5. Weaknesses being corrected

Hardcoded circle → per-frame ellipse+homography. Blown-out metal → exposure bracketing.
Edge-snapped XRF points → interior on-metal points. No multi-item guard → component count.
Open-loop arm → keep affine but recompute registration from the fitted centre each run.
Stones unaccounted → the estimator above. Emgu 2.4 → OpenCvSharp4.
