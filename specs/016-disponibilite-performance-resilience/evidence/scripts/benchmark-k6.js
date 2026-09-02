import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '10s', target: 5 }, // Montée en charge nominale
    { duration: '20s', target: 5 }, // Maintien charge nominale
    { duration: '5s', target: 0 },  // Descente
  ],
  thresholds: {
    http_req_duration: ['p(95)<3000'], // CDC NFR-04 : Seuil contractuel < 3s (3 000 ms)
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';

export default function () {
  const routes = [
    '/healthz',
    '/ready',
    '/comptes/connexion',
  ];

  for (const route of routes) {
    const res = http.get(`${BASE_URL}${route}`);
    check(res, {
      'status is 200': (r) => r.status === 200,
      'response time < 3000ms': (r) => r.timings.duration < 3000,
    });
    sleep(0.5);
  }
}
