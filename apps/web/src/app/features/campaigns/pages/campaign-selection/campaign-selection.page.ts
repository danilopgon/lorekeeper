import { Component, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../../../shared/ui/page-header/page-header.component';
import { toCampaignApiError } from '../../api/campaign-api-error';
import { CampaignCommands } from '../../api/campaign-commands.service';
import { injectCampaignListQuery } from '../../api/campaign.queries';
import { formatCampaignDate } from '../../models/campaign-date';

/** Campaign selection and creation route for the Block 01 workspace entry point. */
@Component({
  imports: [FormsModule, PageHeaderComponent, RouterLink],
  selector: 'app-campaign-selection-page',
  templateUrl: './campaign-selection.page.html',
})
export class CampaignSelectionPage {
  private readonly commands = inject(CampaignCommands);
  private readonly refreshAfterActiveLoad = signal(false);

  protected campaignName = '';
  protected readonly creating = signal(false);
  protected readonly nameError = signal<'campaign_name_invalid' | 'campaign_name_conflict' | null>(
    null,
  );
  protected readonly createRecoverableError = signal(false);
  protected readonly campaigns = injectCampaignListQuery();
  protected readonly campaignCountFolio = computed(() => {
    const count = this.campaigns.hasValue() ? this.campaigns.value().length : 0;
    if (count === 0) {
      return undefined;
    }
    return count === 1 ? '1 campaña' : `${count} campañas`;
  });
  protected readonly formatDate = formatCampaignDate;

  constructor() {
    effect(() => {
      if (this.refreshAfterActiveLoad() && !this.campaigns.isLoading()) {
        this.refreshAfterActiveLoad.set(false);
        this.campaigns.reload();
      }
    });
  }

  /** Creates a campaign without selecting or navigating to it implicitly. */
  protected async createCampaign(): Promise<void> {
    if (this.creating()) {
      return;
    }

    const name = this.campaignName;
    this.startCreate();

    try {
      await this.commands.createCampaign({ name });
      this.handleCreateSuccess();
    } catch (error) {
      this.handleCreateError(error);
    } finally {
      this.creating.set(false);
    }
  }

  /** Maps stable API field error codes to Spanish form copy. */
  protected fieldErrorMessage(code: 'campaign_name_invalid' | 'campaign_name_conflict'): string {
    return code === 'campaign_name_conflict'
      ? 'Ya existe una campaña con ese nombre.'
      : 'Escribe un nombre de entre 1 y 120 caracteres.';
  }

  private startCreate(): void {
    this.creating.set(true);
    this.nameError.set(null);
    this.createRecoverableError.set(false);
  }

  private handleCreateSuccess(): void {
    this.campaignName = '';

    if (this.campaigns.isLoading()) {
      this.refreshAfterActiveLoad.set(true);
      return;
    }

    this.campaigns.reload();
  }

  private handleCreateError(error: unknown): void {
    const apiError = toCampaignApiError(error);

    if (apiError.kind === 'fieldError') {
      this.nameError.set(apiError.code);
      return;
    }

    this.createRecoverableError.set(true);
  }
}
