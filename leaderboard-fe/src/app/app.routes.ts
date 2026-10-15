import { Routes } from '@angular/router';

import { RegisterPage } from './auth/register-page/register-page';
import { LoginPage } from './auth/login-page/login-page';
import { AccountSettings } from './auth/account-settings/account-settings';
import { HomePage } from './home-page/home-page';
import { Games } from './games/games';
import { GameDetail } from './game-detail/game-detail';
import { SearchResults } from './search-results/search-results';
import { UserProfileComponent } from './user-profile/user-profile';
import { PendingScoresComponent } from './pending-scores/pending-scores';
import { MySubmissionsComponent } from './my-submissions/my-submissions';
import { GameModeratorsComponent } from './game-moderators/game-moderators';
import { ScorePostComponent } from './score-post/score-post';
import { ApiKeysComponent } from './api-keys/api-keys';
import { NewGame } from './new-game/new-game';
import { GameConfigure } from './game-configure/game-configure';
import { requireAuth, requireGuest } from './core/clerk-guards';

export const routes: Routes = [
  {
    path: 'auth/register',
    component: RegisterPage,
    canActivate: [requireGuest]
  },
  {
    path: 'auth/register/:step',
    component: RegisterPage,
    canActivate: [requireGuest]
  },
  {
    path: 'auth/login',
    component: LoginPage,
    canActivate: [requireGuest]
  },
  {
    path: 'auth/login/:step',
    component: LoginPage,
    canActivate: [requireGuest]
  },
  {
    path: 'auth/account',
    component: AccountSettings,
    canActivate: [requireAuth]
  },
  {
    path: 'scores/:id',
    component: ScorePostComponent
  },
  {
    path: 'games',
    component: Games
  },
  {
    path: 'games/new',
    component: NewGame,
    canActivate: [requireAuth]
  },
  {
    path: 'games/:id/moderators',
    component: GameModeratorsComponent,
    canActivate: [requireAuth]
  },
  {
    path: 'games/:id/configure',
    component: GameConfigure,
    canActivate: [requireAuth]
  },
  {
    path: 'games/:id/pending-scores',
    component: PendingScoresComponent,
    canActivate: [requireAuth]
  },
  {
    path: 'games/:id/api-keys',
    component: ApiKeysComponent,
    canActivate: [requireAuth]
  },
  {
    path: 'games/:id',
    component: GameDetail
  },
  {
    path: 'my-submissions',
    component: MySubmissionsComponent,
    canActivate: [requireAuth]
  },
  {
    path: 'search',
    component: SearchResults
  },
  {
    path: 'user/:id',
    component: UserProfileComponent
  },
  {
    path: '',
    component: HomePage
  }
];
