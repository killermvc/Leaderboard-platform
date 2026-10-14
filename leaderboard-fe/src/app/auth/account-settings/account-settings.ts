import { Component, ChangeDetectionStrategy } from '@angular/core';
import { ClerkUserProfileComponent } from 'ngx-clerk';

@Component({
  selector: 'app-account-settings',
  imports: [ClerkUserProfileComponent],
  templateUrl: './account-settings.html',
  styleUrl: './account-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AccountSettings {
}
