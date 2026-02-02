import { Component } from '@angular/core';

type ActionItem = {
  icon: string;
  title: string;
  description: string;
};

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  host: {
    'class': 'block'
  }
})
export class HomeComponent {

}
