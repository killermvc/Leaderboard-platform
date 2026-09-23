import { Routes } from '@angular/router';

import { RegisterPage } from './auth/register-page/register-page';
import { LoginPage } from './auth/login-page/login-page';
import { AccountSettings } from './auth/account-settings/account-settings';
import { HomePage } from './home-page/home-page';
import { Games } from './games/games';
import { MyGames } from './my-games/my-games';
import { GameDetail } from './game-detail/game-detail';
import { SearchResults } from './search-results/search-results';
import { UserProfileComponent } from './user-profile/user-profile';
import { PendingScoresComponent } from './pending-scores/pending-scores';
import { MySubmissionsComponent } from './my-submissions/my-submissions';
import { GameModeratorsComponent } from './game-moderators/game-moderators';
import { ScorePostComponent } from './score-post/score-post';
import { ApiKeysComponent } from './api-keys/api-keys';

export const routes: Routes = [
	  {
		path: 'auth/register',
		component: RegisterPage
	  },
	  {
		path: 'auth/login',
		component: LoginPage
	  },
	  {
		path: 'auth/account',
		component: AccountSettings
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
		path: 'games/:id/moderators',
		component: GameModeratorsComponent
	  },
	  {
		path: 'games/:id/pending-scores',
		component: PendingScoresComponent
	  },
	  {
		path: 'games/:id/api-keys',
		component: ApiKeysComponent
	  },
	  {
		path: 'games/:id',
		component: GameDetail
	  },
	  {
		path: 'mygames',
		component: MyGames
	  },
	  {
		path: 'my-submissions',
		component: MySubmissionsComponent
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
