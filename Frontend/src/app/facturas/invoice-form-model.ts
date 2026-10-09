import { FormArray, FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
export const MAXIMUM_INVOICE_MONEY = 999_999_999_999.99;
export const positiveInteger: ValidatorFn = (control) =>
  control.value == null ||
  (Number.isInteger(control.value) && control.value > 0 && control.value <= 2147483647)
    ? null
    : { integer: true };
export const money: ValidatorFn = (control) =>
  control.value == null ||
  (typeof control.value === 'number' &&
    control.value > 0 &&
    control.value <= MAXIMUM_INVOICE_MONEY &&
    /^\d+(\.\d{1,2})?$/.test(String(control.value)))
    ? null
    : { money: true };
export function invoiceLineForm() {
  return new FormGroup({
    code: new FormControl<number | null>(null, [Validators.required, positiveInteger]),
    quantity: new FormControl<number | null>(1, [Validators.required, positiveInteger]),
    unitPrice: new FormControl<number | null>(null, [Validators.required, money]),
  });
}
const text = (max: number) =>
  new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(max), Validators.pattern(/\S/)],
  });
export function invoiceForm() {
  return new FormGroup({
    document: text(15),
    firstName: text(100),
    lastName: text(100),
    address: text(200),
    phone: text(30),
    details: new FormArray([invoiceLineForm()]),
  });
}
