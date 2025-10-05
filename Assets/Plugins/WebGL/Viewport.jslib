mergeInto(LibraryManager.library, {
  GetViewportWidth: function() {
    return window.innerWidth;
  },
  GetViewportHeight: function() {
    return window.innerHeight;
  }
});
