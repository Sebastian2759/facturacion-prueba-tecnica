import { Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-form-field',
  imports: [ReactiveFormsModule],
  template: `
    <label [for]="fieldId()"
      >{{ label() }}
      @if (required()) {
        <span aria-hidden="true"> *</span>
      }
    </label>
    <input
      [id]="fieldId()"
      [type]="type()"
      [formControl]="control()"
      [attr.maxlength]="maxLength()"
      [attr.autocomplete]="autocomplete()"
      [attr.inputmode]="inputMode()"
      [placeholder]="placeholder()"
      [attr.aria-required]="required()"
      [attr.aria-invalid]="invalfieldId()"
      [attr.aria-describedby]="fieldId() + '-hint'"
    />
    <div [id]="fieldId() + '-hint'" class="field-hint" [class.field-error]="invalfieldId()">
      @if (serverError()) {
        {{ serverError() }}
      } @else if (control().touched && control().invalid) {
        @if (control().hasError('required')) {
          Este campo es obligatorio.
        } @else if (control().hasError('email')) {
          Escribe un correo válido.
        } @else {
          Revisa el formato y la longitud de este campo.
        }
      } @else {
        {{ hint() }}
      }
    </div>
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
    label {
      display: block;
      font-size: 0.79rem;
      font-weight: 600;
      margin-bottom: 0.4rem;
    }
    label span {
      color: var(--muted);
    }
  `,
})
export class FormField {
  readonly fieldId = input.required<string>();
  readonly label = input.required<string>();
  readonly control = input.required<FormControl<string>>();
  readonly required = input(false);
  readonly type = input('text');
  readonly maxLength = input<number | null>(null);
  readonly autocomplete = input('off');
  readonly inputMode = input('text');
  readonly placeholder = input('');
  readonly hint = input('');
  readonly serverError = input('');
  protected invalfieldId(): boolean {
    return !!this.serverError() || (this.control().touched && this.control().invalid);
  }
}
