import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { TodoRecurrenceType } from '../../model/todo';

@Component({
  selector: 'app-todo-schedule-dialog',
  templateUrl: './todo-schedule-dialog.component.html',
  styleUrls: ['./todo-schedule-dialog.component.scss'],
})
export class TodoScheduleDialogComponent {
  @Input() form!: FormGroup;
  @Input() isOpen = false;
  @Input() weekDays: { value: number; label: string; short: string }[] = [];
  @Input() recurrenceOptions: { value: TodoRecurrenceType; label: string }[] = [];
  @Output() closed = new EventEmitter<void>();

  TodoRecurrenceType = TodoRecurrenceType;

  close(): void {
    this.closed.emit();
  }

  onRecurrenceTypeChange(): void {
    const type = this.form.get('RecurrenceType')?.value;
    if (type !== TodoRecurrenceType.Weekly) {
      this.form.patchValue({ RecurrenceDaysOfWeek: [] }, { emitEvent: false });
    }
    if (type === TodoRecurrenceType.None) {
      this.form.patchValue({ RecurrenceInterval: 1, RecurrenceEndsOn: null, ApplyToSeries: false }, { emitEvent: false });
    }
  }

  toggleDaySelection(day: number): void {
    const control = this.form.get('RecurrenceDaysOfWeek');
    if (!control) {
      return;
    }
    const current: number[] = Array.isArray(control.value) ? control.value : [];
    const exists = current.includes(day);
    const updated = exists ? current.filter((d) => d !== day) : [...current, day];
    control.setValue(updated);
  }

  isDaySelected(day: number): boolean {
    const control = this.form.get('RecurrenceDaysOfWeek');
    if (!control) {
      return false;
    }
    const current: number[] = Array.isArray(control.value) ? control.value : [];
    return current.includes(day);
  }
}
