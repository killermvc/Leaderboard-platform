import { Component } from '@angular/core';
import { ClerkSignUpComponent } from 'ngx-clerk';

@Component({
  selector: 'app-register-page',
	imports: [ClerkSignUpComponent],
  templateUrl: './register-page.html',
  styleUrls: ['./register-page.scss'],
})
export class RegisterPage {
}
