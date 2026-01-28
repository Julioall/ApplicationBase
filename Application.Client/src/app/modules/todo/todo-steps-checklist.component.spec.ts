import { DragDropModule } from '@angular/cdk/drag-drop';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { TodoStepsChecklistComponent } from './todo-steps-checklist.component';
import { TodoStep } from '../../model/todo';

describe('TodoStepsChecklistComponent', () => {
  let component: TodoStepsChecklistComponent;
  let fixture: ComponentFixture<TodoStepsChecklistComponent>;

  const steps: TodoStep[] = [
    { Id: 'steps/1', Title: 'First', IsCompleted: false, Order: 0 },
    { Id: 'steps/2', Title: 'Second', IsCompleted: false, Order: 1 },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [TodoStepsChecklistComponent],
      imports: [DragDropModule, FormsModule, TranslateModule.forRoot()],
    }).compileComponents();

    fixture = TestBed.createComponent(TodoStepsChecklistComponent);
    component = fixture.componentInstance;
    component.steps = [...steps];
    fixture.detectChanges();
  });

  it('should emit reorder on drop', () => {
    spyOn(component.reorder, 'emit');
    const event: any = { previousIndex: 0, currentIndex: 1 };

    component.onDrop(event);

    expect(component.reorder.emit).toHaveBeenCalled();
    const emitted = (component.reorder.emit as jasmine.Spy).calls.mostRecent().args[0] as TodoStep[];
    expect(emitted[0].Id).toBe('steps/2');
    expect(emitted[0].Order).toBe(0);
  });

  it('should emit stepToggle event', () => {
    spyOn(component.stepToggle, 'emit');
    component.onToggle(steps[0], true);
    expect(component.stepToggle.emit).toHaveBeenCalledWith({ stepId: steps[0].Id, isCompleted: true });
  });

  it('should emit addStep when submitting new step', () => {
    spyOn(component.addStep, 'emit');
    component.newStepTitle = 'New step';
    component.submitNewStep();
    expect(component.addStep.emit).toHaveBeenCalledWith('New step');
  });
});
