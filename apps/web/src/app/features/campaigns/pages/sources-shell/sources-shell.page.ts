import { Component } from '@angular/core';
import { CampaignWorkspaceShellComponent } from '../campaign-workspace-shell/campaign-workspace-shell.component';

/** Sources route shell for a campaign workspace while ingestion capabilities are unavailable. */
@Component({
  imports: [CampaignWorkspaceShellComponent],
  selector: 'app-sources-shell-page',
  templateUrl: './sources-shell.page.html',
})
export class SourcesShellPage {}
