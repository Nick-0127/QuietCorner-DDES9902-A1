mergeInto(LibraryManager.library, {
  $QuietCornerMouse: {
    canvas: null, active: false, locked: false, inside: false,
    x: 0, y: 0, previous: false, dx: 0, dy: 0, clicks: 0,
    repositioning: false, lockPending: false, lockError: '',
    describe: function() {
      if (!this.canvas) return;
      this.canvas.setAttribute('data-qc-mouse-mode', this.active ? (this.locked ? 'captured' : 'mouse-follow') : 'released');
      this.canvas.setAttribute('data-qc-pointer-lock-error', this.lockError);
      this.canvas.setAttribute('data-qc-repositioning', this.repositioning ? 'true' : 'false');
      this.canvas.style.cursor = this.active && !this.repositioning ? 'none' : 'default';
    },
    requestCapture: function() {
      if (!this.active || this.locked || this.lockPending || this.repositioning) return;
      var s = this;
      if (!s.canvas.requestPointerLock) { s.lockError = 'unavailable'; s.describe(); return; }
      s.lockPending = true;
      try {
        var request = s.canvas.requestPointerLock();
        if (request && request.then) {
          request.then(function() { s.lockPending = false; }, function(error) {
            s.lockPending = false;
            s.lockError = error && error.name ? error.name : 'denied';
            s.describe();
          });
        } else s.lockPending = false;
      } catch (error) {
        s.lockPending = false;
        s.lockError = error && error.name ? error.name : 'denied';
        s.describe();
      }
    },
    release: function() {
      this.active = false; this.inside = false; this.previous = false;
      this.repositioning = false;
      this.dx = this.dy = this.clicks = 0;
      if (document.pointerLockElement === this.canvas) document.exitPointerLock();
      this.locked = false;
      this.describe();
    },
    position: function(e) {
      var r = this.canvas.getBoundingClientRect();
      this.inside = e.clientX >= r.left && e.clientX <= r.right && e.clientY >= r.top && e.clientY <= r.bottom;
      this.x = (e.clientX - r.left) / r.width;
      this.y = (e.clientY - r.top) / r.height;
      return r;
    },
    init: function() {
      if (this.canvas) return;
      var s = this;
      s.canvas = Module['canvas'];
      s.describe();
      s.canvas.addEventListener('mousedown', function(e) {
        if (e.button === 2 && s.active && !s.locked) {
          s.repositioning = true; s.dx = s.dy = s.clicks = 0;
          s.position(e); s.previous = true; s.describe();
          e.preventDefault(); return;
        }
        if (e.button !== 0) return;
        if (s.active) {
          if (!s.repositioning) s.clicks++;
          s.requestCapture(); return;
        }
        s.active = true;
        s.canvas.focus();
        s.position(e); s.previous = true;
        s.dx = s.dy = s.clicks = 0;
        s.describe();
        // Retry from actual user gestures if an earlier capture request failed.
        s.requestCapture();
      });
      s.canvas.addEventListener('click', function(e) { if (e.button === 0) s.requestCapture(); });
      s.canvas.addEventListener('contextmenu', function(e) { if (s.active) e.preventDefault(); });
      document.addEventListener('mouseup', function(e) {
        if (e.button !== 2 || !s.repositioning) return;
        s.position(e); s.previous = s.inside;
        s.repositioning = false; s.dx = s.dy = 0; s.describe();
      });
      document.addEventListener('pointerlockchange', function() {
        var captured = document.pointerLockElement === s.canvas;
        if (captured && !s.active) { document.exitPointerLock(); return; }
        if (!captured && s.locked) { s.release(); return; }
        s.locked = captured;
        s.lockPending = false;
        if (captured) { s.lockError = ''; s.repositioning = false; }
        s.previous = false; s.dx = s.dy = 0;
        s.describe();
      });
      document.addEventListener('pointerlockerror', function() {
        s.lockPending = false; s.lockError = 'denied'; s.describe();
      });
      document.addEventListener('mousemove', function(e) {
        if (!s.active) return;
        var oldX = s.x, oldY = s.y, wasInside = s.inside;
        var r = s.position(e);
        if (s.repositioning) {
          // Hold RMB to move the physical pointer back without undoing the turn.
          s.previous = s.inside; return;
        } else if (s.locked) {
          s.dx += (e.movementX || 0) / r.height;
          s.dy -= (e.movementY || 0) / r.height;
        } else if (s.inside && wasInside && s.previous) {
          // A canvas-width sweep maps to 420 degrees. Unity Play's embed denies
          // pointer lock; recenter with RMB and repeat for a complete rotation.
          s.dx += (s.x - oldX) * 4.2;
          s.dy -= s.y - oldY;
        }
        s.previous = s.inside;
      });
      s.canvas.addEventListener('mouseleave', function() {
        if (!s.locked) { s.inside = false; s.previous = false; }
      });
      document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape') { s.release(); return; }
        if ((e.key === 'r' || e.key === 'R') && s.active && !s.locked) {
          // Toggle a no-turn reposition mode for hosts that deny pointer lock.
          s.repositioning = !s.repositioning;
          s.previous = s.inside;
          s.dx = s.dy = s.clicks = 0;
          s.describe();
          e.preventDefault();
        }
      });
      window.addEventListener('blur', function() { s.release(); });
      document.addEventListener('visibilitychange', function() { if (document.hidden) s.release(); });
    },
    read: function(dt) {
      var dx = this.dx, dy = this.dy;
      this.dx = this.dy = 0;
      // Only actual mouse movement changes the view. A stationary pointer,
      // including at a canvas edge when capture is unavailable, adds no turn.
      var click = this.clicks > 0;
      this.clicks = 0;
      return [this.active ? dx : 0, this.active ? dy : 0, this.active ? 1 : 0, click ? 1 : 0, this.locked ? 1 : 0];
    }
  },
  QC_MouseInit__deps: ['$QuietCornerMouse'],
  QC_MouseInit: function() { QuietCornerMouse.init(); },
  QC_MouseRead__deps: ['$QuietCornerMouse'],
  QC_MouseRead: function(output, dt) {
    var values = QuietCornerMouse.read(dt);
    for (var i = 0; i < 5; i++) HEAPF32[(output >> 2) + i] = values[i];
  },
  QC_MouseRelease__deps: ['$QuietCornerMouse'],
  QC_MouseRelease: function() { QuietCornerMouse.release(); }
});
