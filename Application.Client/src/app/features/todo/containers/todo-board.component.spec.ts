import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TodoBoardComponent } from './todo-board.component';
import { TodoFacadeService } from '../services/todo-facade.service';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

describe('TodoBoardComponent', () => {
  let component: TodoBoardComponent;
  let fixture: ComponentFixture<TodoBoardComponent>;
  let todoFacade: TodoFacadeService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TodoBoardComponent, BrowserAnimationsModule],
      providers: [TodoFacadeService]
    }).compileComponents();

    fixture = TestBed.createComponent(TodoBoardComponent);
    component = fixture.componentInstance;
    todoFacade = TestBed.inject(TodoFacadeService);
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(component).toBeTruthy();
  });

  it('deve carregar tarefas ao inicializar', (done) => {
    component.todos$.subscribe(todos => {
      if (todos.length > 0) {
        expect(todos.length).toBeGreaterThan(0);
        done();
      }
    });
  });

  it('deve selecionar uma tarefa ao clicar', () => {
    const mockTodo = {
      id: '1',
      title: 'Test',
      completed: false,
      priority: 'high' as const
    };

    component.onSelectTodo(mockTodo);

    component.selectedTodo$.subscribe(selected => {
      expect(selected).toEqual(mockTodo);
    });
  });

  it('deve limpar seleção', () => {
    component.onClearSelection();

    component.selectedTodo$.subscribe(selected => {
      expect(selected).toBeNull();
    });
  });

  it('deve retornar variante correta de prioridade', () => {
    expect(component.getPriorityVariant('high')).toBe('error');
    expect(component.getPriorityVariant('medium')).toBe('warning');
    expect(component.getPriorityVariant('low')).toBe('success');
  });
});
