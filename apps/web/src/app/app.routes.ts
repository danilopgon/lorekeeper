import { Routes } from '@angular/router';
import { CampaignSelectionPage } from './features/campaigns/pages/campaign-selection/campaign-selection.page';
import { ChatShellPage } from './features/campaigns/pages/chat-shell/chat-shell.page';
import { SourcesShellPage } from './features/campaigns/pages/sources-shell/sources-shell.page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'campaigns' },
  { path: 'campaigns', title: 'Campañas — Lorekeeper', component: CampaignSelectionPage },
  { path: 'campaigns/:campaignId/chat', title: 'Chat — Lorekeeper', component: ChatShellPage },
  {
    path: 'campaigns/:campaignId/sources',
    title: 'Fuentes — Lorekeeper',
    component: SourcesShellPage,
  },
];
