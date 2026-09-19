import { Routes } from '@angular/router';
import { CampaignSelectionPage } from './features/campaigns/pages/campaign-selection/campaign-selection.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'campaigns' },
  { path: 'campaigns', component: CampaignSelectionPage },
];
