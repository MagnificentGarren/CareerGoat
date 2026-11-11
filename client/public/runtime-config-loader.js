if (['localhost', '127.0.0.1', '[::1]'].includes(window.location.hostname)) {
  document.write('<script src="runtime-config.local.js"><\/script>');
}
