const developmentOrigin = 'http://127.0.0.1:4200';
const targetOrigin = 'http://127.0.0.1:5080';

// Development only. The packaged application serves UI/API on targetOrigin directly.
module.exports = {
  '/api': {
    target: targetOrigin,
    secure: false,
    changeOrigin: true,
    bypass(request) {
      const origin = request.headers.origin;
      const isRead = ['GET', 'HEAD', 'OPTIONS'].includes(request.method);
      // Reject foreign origins before rewriting: never turn a cross-site write into a trusted one.
      if (
        request.headers.host !== '127.0.0.1:4200' ||
        (origin !== undefined && origin !== developmentOrigin) ||
        (!isRead && origin !== developmentOrigin)
      ) {
        return false;
      }
      return undefined;
    },
    configure(proxy) {
      proxy.on('proxyReq', (outgoing, incoming) => {
        if (incoming.headers.origin === developmentOrigin) {
          outgoing.setHeader('Origin', targetOrigin);
        }
      });
    },
  },
};
