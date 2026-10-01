import { Component } from '@angular/core';
import { ClerkSignInComponent } from 'ngx-clerk';

@Component({
  selector: 'app-login-page',
	imports: [ClerkSignInComponent],
  templateUrl: './login-page.html',
  styleUrls: ['./login-page.scss'],
})
export class LoginPage {
}
