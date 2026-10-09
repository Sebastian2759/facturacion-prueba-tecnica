import { describe, expect, it } from 'vitest';
import { invoiceForm, invoiceLineForm, MAXIMUM_INVOICE_MONEY } from './invoice-form-model';
describe('Invoice input contract', () => {
  it('requires all adopted customer fields and a complete item', () => {
    const form = invoiceForm();
    expect(form.valid).toBe(false);
    form.setValue({
      document: 'TEST-1',
      firstName: 'Cliente',
      lastName: 'Ficticio',
      address: 'Calle de ejemplo',
      phone: '3000000000',
      details: [{ code: 1, quantity: 2, unitPrice: 10.25 }],
    });
    expect(form.valid).toBe(true);
    form.controls.firstName.setValue('   ');
    expect(form.valid).toBe(false);
  });
  it('rejects fractional integers and prices with more than two decimals', () => {
    const line = invoiceLineForm();
    line.setValue({ code: 1, quantity: 1.5, unitPrice: 0.001 });
    expect(line.controls.quantity.invalid).toBe(true);
    expect(line.controls.unitPrice.invalid).toBe(true);
    line.setValue({ code: 2147483648, quantity: 1, unitPrice: 0.01 });
    expect(line.controls.code.invalid).toBe(true);
    expect(line.controls.unitPrice.valid).toBe(true);
  });
  it('rejects documents longer than the SQL VARCHAR(15) contract', () => {
    const form = invoiceForm();
    form.controls.document.setValue('1234567890123456');
    expect(form.controls.document.hasError('maxlength')).toBe(true);
  });
  it('enforces the shared price cap while preserving two decimal places at the boundary', () => {
    const price = invoiceLineForm().controls.unitPrice;
    price.setValue(MAXIMUM_INVOICE_MONEY);
    expect(price.valid).toBe(true);
    expect(JSON.parse(JSON.stringify(MAXIMUM_INVOICE_MONEY)).toFixed(2)).toBe('999999999999.99');
    price.setValue(1_000_000_000_000);
    expect(price.hasError('money')).toBe(true);
  });
});
