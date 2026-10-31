import { Component, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

/** Root application shell component. */
@Component({
  imports: [RouterLink, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.component.css',
  templateUrl: './app.component.html',
})
export class AppComponent {
  protected readonly title = signal('Lorekeeper');
}
