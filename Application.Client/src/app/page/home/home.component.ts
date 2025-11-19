import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../service/auth/auth.service';

type NavItem = {
  icon: string;
  label: string;
  badge?: string;
  active?: boolean;
};

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent implements OnInit {
  token: string | null | undefined;
  isLoggedIn = false;
  isNavOpen = false;
  primaryNav: NavItem[] = [
    { icon: 'fa-solid fa-compass', label: 'Visão geral', active: true },
    { icon: 'fa-solid fa-list-check', label: 'Projetos' },
    { icon: 'fa-solid fa-table-columns', label: 'Boards' },
    { icon: 'fa-solid fa-users', label: 'Times' },
    { icon: 'fa-solid fa-chart-simple', label: 'Relatórios' },
    { icon: 'fa-solid fa-robot', label: 'Automação' },
  ];

  favoriteNav: NavItem[] = [
    { icon: 'fa-regular fa-star', label: 'Design System', badge: 'UI' },
    { icon: 'fa-regular fa-star', label: 'Mobile App', badge: 'Sprint' },
    { icon: 'fa-regular fa-star', label: 'Service Desk', badge: 'Suporte' }
  ];

  quickLinks: NavItem[] = [
    { icon: 'fa-regular fa-note-sticky', label: 'Documentação' },
    { icon: 'fa-solid fa-bolt', label: 'Automação' },
    { icon: 'fa-solid fa-flag', label: 'Roadmap' }
  ];

  cards = [
    { icon: 'fa-solid fa-diagram-project', title: 'Projetos', description: 'Gerencie iniciativas, backlogs e releases sem sair do dashboard.' },
    { icon: 'fa-solid fa-layer-group', title: 'Boards Kanban', description: 'Visualize o fluxo das tarefas e veja gargalos rapidamente.' },
    { icon: 'fa-solid fa-users-gear', title: 'Times e permissões', description: 'Controle acesso e visibilidade dos times com poucos cliques.' },
    { icon: 'fa-solid fa-chart-pie', title: 'Relatórios', description: 'Métricas de throughput, lead time e burndown sempre atualizadas.' }
  ];

  constructor(
    private authService: AuthService,
    private router: Router

  ) {}

  ngOnInit() {
    this.isLoggedIn = this.authService.isLoggedIn();
    this.token = this.authService.getToken();
    if (!this.isLoggedIn) {
      this.router.navigate(['/auth']);
    }
  }

  toggleNav(): void {
    this.isNavOpen = !this.isNavOpen;
  }

  closeNav(): void {
    this.isNavOpen = false;
  }
}
