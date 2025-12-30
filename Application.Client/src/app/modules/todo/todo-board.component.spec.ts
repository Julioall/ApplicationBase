import { DragDropModule } from '@angular/cdk/drag-drop';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { of } from 'rxjs';
import { AuthService } from '../../service/auth/auth.service';
import { TodoService } from '../../service/todo/todo.service';
import { TodoStatus, TodoTask } from '../../model/todo';
import { TodoBoardComponent } from './todo-board.component';

describe('TodoBoardComponent', () => {
  let component: TodoBoardComponent;
  let fixture: ComponentFixture<TodoBoardComponent>;
  let todoService: jasmine.SpyObj<TodoService>;
  let mockTasks: TodoTask[];

  const authStub: Partial<AuthService> = {
    hasPermission: () => true,
    hasAnyPermission: () => true,
  };

  beforeEach(async () => {
    mockTasks = [
      {
        Id: 'tasks/1-A',
        Title: 'Draft syllabus',
        Status: TodoStatus.NotStarted,
        CreatedAt: new Date().toISOString(),
        IsArchived: false,
        Steps: [],
      },
      {
        Id: 'tasks/2-A',
        Title: 'Publish results',
        Status: TodoStatus.InProgress,
        CreatedAt: new Date().toISOString(),
        IsArchived: false,
        Steps: [],
      },
    ];

    todoService = jasmine.createSpyObj<TodoService>('TodoService', ['getTasks', 'createTask', 'updateTask']);
    todoService.getTasks.and.returnValue(of(mockTasks));
    todoService.updateTask.and.callFake((id, dto) =>
      of({
        ...mockTasks.find((t) => t.Id === id)!,
        Status: (dto as any).Status ?? TodoStatus.NotStarted,
      })
    );

    await TestBed.configureTestingModule({
      declarations: [TodoBoardComponent],
      imports: [ReactiveFormsModule, FormsModule, DragDropModule, TranslateModule.forRoot(), HttpClientTestingModule],
      providers: [
        { provide: TodoService, useValue: todoService },
        { provide: AuthService, useValue: authStub },
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(TodoBoardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load tasks into columns', fakeAsync(() => {
    component.ngOnInit();
    tick();
    fixture.detectChanges();

    expect(todoService.getTasks).toHaveBeenCalled();
    expect(component.tasks.length).toBe(mockTasks.length);
    expect(component.columns.length).toBe(3);
    const notStarted = component.columns.find((c) => c.status === TodoStatus.NotStarted);
    expect(notStarted?.items.length).toBe(1);
  }));

  it('should update status on drop', () => {
    const source = component.columns.find((c) => c.status === TodoStatus.NotStarted)!;
    const target = component.columns.find((c) => c.status === TodoStatus.Done)!;
    const event: any = {
      previousContainer: { data: source.items },
      container: { data: target.items },
      previousIndex: 0,
      currentIndex: 0,
    };

    component.onDrop(event, TodoStatus.Done);

    expect(todoService.updateTask).toHaveBeenCalled();
    const args = todoService.updateTask.calls.mostRecent().args;
    expect(args[0]).toBe('tasks/1-A');
    expect((args[1] as any).Status).toBe(TodoStatus.Done);
  });
});
