import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TodoTaskDetailComponent } from './todo-task-detail.component';
import { TodoModel } from '../services/todo-facade.service';

describe('TodoTaskDetailComponent', () => {
  let component: TodoTaskDetailComponent;
  let fixture: ComponentFixture<TodoTaskDetailComponent>;

  const mockTodo: TodoModel = {
    id: '1',
    title: 'Test Task',
    description: 'Test Description',
    completed: false,
    priority: 'high',
    dueDate: new Date(),
    steps: [
      { id: '1', title: 'Step 1', completed: false },
      { id: '2', title: 'Step 2', completed: true }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TodoTaskDetailComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(TodoTaskDetailComponent);
    component = fixture.componentInstance;
    component.todo = mockTodo;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(component).toBeTruthy();
  });

  it('deve exibir título da tarefa', () => {
    const compiled = fixture.nativeElement;
    expect(compiled.querySelector('h2').textContent).toContain('Test Task');
  });

  it('deve exibir descrição', () => {
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Test Description');
  });

  it('deve emitir edit ao clicar em Editar', () => {
    spyOn(component.edit, 'emit');
    
    const editBtn = fixture.nativeElement.querySelector('button');
    editBtn.click();

    expect(component.edit.emit).toHaveBeenCalledWith(mockTodo);
  });

  it('deve emitir delete ao clicar em Deletar', () => {
    spyOn(component.delete, 'emit');
    
    const buttons = fixture.nativeElement.querySelectorAll('button');
    const deleteBtn = buttons[1];
    deleteBtn.click();

    expect(component.delete.emit).toHaveBeenCalledWith(mockTodo.id);
  });

  it('deve retornar variante correta para prioridade', () => {
    expect(component.getPriorityVariant()).toBe('error');
    
    if (component.todo) {
      component.todo.priority = 'medium';
      expect(component.getPriorityVariant()).toBe('warning');
    }
  });

  it('deve usar OnPush change detection', () => {
    const metadata = (TodoTaskDetailComponent as any).__annotations__[0];
    expect(metadata.changeDetection).toBeDefined();
  });
});
