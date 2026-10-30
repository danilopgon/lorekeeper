import { Component, input } from '@angular/core';

/** Codex page header: an optional fact-bearing folio line, the display title and projected lead content. */
@Component({
  selector: 'app-page-header',
  templateUrl: './page-header.component.html',
})
export class PageHeaderComponent {
  /** Short fact or identifier shown above the title; omitted when there is no real fact to show. */
  readonly folio = input<string>();
  readonly heading = input.required<string>();
  readonly headingId = input.required<string>();
}
