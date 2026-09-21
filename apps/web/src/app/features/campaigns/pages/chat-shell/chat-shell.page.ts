import { Component } from '@angular/core';
import { CampaignWorkspaceShellComponent } from '../campaign-workspace-shell/campaign-workspace-shell.component';

/** Chat route shell for a campaign workspace while chat capabilities are unavailable. */
@Component({
  imports: [CampaignWorkspaceShellComponent],
  selector: 'app-chat-shell-page',
  templateUrl: './chat-shell.page.html',
})
export class ChatShellPage {}
