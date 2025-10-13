mergeInto(LibraryManager.library, {
  IsMobileDevice: function() {
    var userAgent = navigator.userAgent || navigator.vendor || window.opera;
    var isMobileUA = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent.toLowerCase());
    
    return isMobileUA ? 1 : 0;
  }
});
