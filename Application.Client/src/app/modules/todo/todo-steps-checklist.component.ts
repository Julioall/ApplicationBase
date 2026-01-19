import { CdkDragDrop, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TodoStep } from '../../model/todo';

@Component({
  selector: 'app-todo-steps-checklist',
  templateUrl: './todo-steps-checklist.component.html',
  styleUrls: ['./todo-steps-checklist.component.scss'],
})
export class TodoStepsChecklistComponent {
  @Input() steps: TodoStep[] = [];
  @Input() canEdit = true;
  @Input() canToggle = true;

  @Output() reorder = new EventEmitter<TodoStep[]>();
  @Output() toggle = new EventEmitter<{ stepId: string; isCompleted: boolean }>();
  @Output() titleChange = new EventEmitter<{ stepId: string; newTitle: string }>();
  @Output() addStep = new EventEmitter<string>();
  @Output() deleteStep = new EventEmitter<string>();

  editingStepId: string | null = null;
  editingTitle = '';
  newStepTitle = '';

  onDrop(event: CdkDragDrop<TodoStep[]>): void {
    if (event.previousIndex === event.currentIndex || !this.canEdit) {
      return;
    }

    moveItemInArray(this.steps, event.previousIndex, event.currentIndex);
    this.steps = this.steps.map((step, index) => ({ ...step, Order: index }));
    this.reorder.emit([...this.steps]);
  }

  onToggle(step: TodoStep, checked: boolean): void {
    if (!this.canToggle) {
      return;
    }
    this.toggle.emit({ stepId: step.Id, isCompleted: checked });
  }

  startEditing(step: TodoStep): void {
    if (!this.canEdit) {
      return;
    }
    this.editingStepId = step.Id;
    this.editingTitle = step.Title;
  }

  commitEdit(step: TodoStep): void {
    if (!this.canEdit) {
      return;
    }
    const title = this.editingTitle?.trim();
    this.editingStepId = null;
    if (title && title !== step.Title) {
      this.titleChange.emit({ stepId: step.Id, newTitle: title });
    }
  }

  submitNewStep(): void {
    if (!this.canEdit) {
      return;
    }

    const title = this.newStepTitle.trim();
    if (!title) {
      return;
    }

    this.addStep.emit(title);
    this.newStepTitle = '';
  }

  trackById(_: number, step: TodoStep): string {
    return step.Id;
  }
}
