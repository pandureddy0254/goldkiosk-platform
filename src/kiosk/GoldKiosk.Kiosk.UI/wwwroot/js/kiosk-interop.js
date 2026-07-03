// GoldKiosk kiosk-mode interop: chrome suppression + the signature draw canvas.
// The Blazor side calls window.kioskInterop.signature.* via IJSRuntime.

(function () {
  "use strict";

  // Kiosk chrome suppression: no context menu, no text selection, no drag.
  document.addEventListener("contextmenu", function (e) { e.preventDefault(); });
  document.addEventListener("selectstart", function (e) { e.preventDefault(); });
  document.addEventListener("dragstart", function (e) { e.preventDefault(); });

  // Fixed 9:16 stage (1080x1920) scaled to whatever display the kiosk has.
  function updateStageScale() {
    var scale = Math.min(window.innerWidth / 1080, window.innerHeight / 1920);
    document.documentElement.style.setProperty("--stage-scale", String(scale));
  }
  window.addEventListener("resize", updateStageScale);
  updateStageScale();

  var signatures = {};

  function getContext(canvas) {
    var ctx = canvas.getContext("2d");
    ctx.lineWidth = 3.5;
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.strokeStyle = "#E8EDF5";
    return ctx;
  }

  function pointFromEvent(canvas, e) {
    var rect = canvas.getBoundingClientRect();
    return {
      x: (e.clientX - rect.left) * (canvas.width / rect.width),
      y: (e.clientY - rect.top) * (canvas.height / rect.height)
    };
  }

  window.kioskInterop = {
    signature: {
      init: function (canvasId) {
        var canvas = document.getElementById(canvasId);
        if (!canvas || signatures[canvasId]) {
          return;
        }
        var state = { drawing: false, strokes: 0 };
        signatures[canvasId] = state;
        var ctx = getContext(canvas);

        canvas.addEventListener("pointerdown", function (e) {
          e.preventDefault();
          canvas.setPointerCapture(e.pointerId);
          state.drawing = true;
          state.strokes++;
          var p = pointFromEvent(canvas, e);
          ctx.beginPath();
          ctx.moveTo(p.x, p.y);
        });
        canvas.addEventListener("pointermove", function (e) {
          if (!state.drawing) {
            return;
          }
          var p = pointFromEvent(canvas, e);
          ctx.lineTo(p.x, p.y);
          ctx.stroke();
        });
        var end = function (e) {
          if (state.drawing) {
            state.drawing = false;
            ctx.closePath();
          }
        };
        canvas.addEventListener("pointerup", end);
        canvas.addEventListener("pointercancel", end);
        canvas.addEventListener("pointerleave", end);
      },

      clear: function (canvasId) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) {
          return;
        }
        var ctx = canvas.getContext("2d");
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        if (signatures[canvasId]) {
          signatures[canvasId].strokes = 0;
        }
      },

      isEmpty: function (canvasId) {
        var state = signatures[canvasId];
        return !state || state.strokes === 0;
      },

      toDataUrl: function (canvasId) {
        var canvas = document.getElementById(canvasId);
        return canvas ? canvas.toDataURL("image/png") : "";
      }
    }
  };
})();
