/** Keeps an ambiguous retry bound to the exact serialized payload. Never persisted with PII. */
export class Idempotency {
  private body?: string;
  private key?: string;
  forBody(value: unknown): string {
    const body = JSON.stringify(value);
    if (this.body !== body) {
      this.body = body;
      this.key = crypto.randomUUID();
    }
    return this.key!;
  }
}
