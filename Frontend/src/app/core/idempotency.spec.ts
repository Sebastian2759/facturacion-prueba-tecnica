import { describe, expect, it } from 'vitest';
import { Idempotency } from './idempotency';
describe('Invoice retry identity', () => {
  it('reuses the same UUID after an ambiguous result with the exact same payload', () => {
    const retry = new Idempotency();
    const key = retry.forBody({
      document: 'DEMO',
      details: [{ code: 1, quantity: 1, unitPrice: 10 }],
    });
    expect(
      retry.forBody({ document: 'DEMO', details: [{ code: 1, quantity: 1, unitPrice: 10 }] }),
    ).toBe(key);
    expect(key).toMatch(/^[0-9a-f-]{36}$/);
  });
  it('does not reuse an old key for changed customer or item data', () => {
    const retry = new Idempotency();
    expect(retry.forBody({ quantity: 1 })).not.toBe(retry.forBody({ quantity: 2 }));
  });
});
