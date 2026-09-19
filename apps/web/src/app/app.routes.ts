import { Routes } from '@angular/router';
import { CampaignSelectionPage } from './features/campaigns/pages/campaign-selection/campaign-selection.page';
import { WorkspaceUnavailablePage } from './features/campaigns/pages/workspace-unavailable/workspace-unavailable.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'campaigns' },
  { path: 'campaigns', component: CampaignSelectionPage },
  { path: 'campaigns/:campaignId/chat', component: WorkspaceUnavailablePage },
  { path: 'campaigns/:campaignId/sources', component: WorkspaceUnavailablePage },
];
