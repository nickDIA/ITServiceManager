import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

/** Placeholder para validar roleGuard(['Admin']) — solo Admin llega hasta aquí. */
@Component({
  selector: 'app-admin',
  imports: [],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.css'
})
export class AdminComponent {
  readonly authService = inject(AuthService);
}
