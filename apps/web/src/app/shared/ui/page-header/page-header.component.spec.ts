import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PageHeaderComponent } from './page-header.component';

@Component({
  imports: [PageHeaderComponent],
  template: `
    <app-page-header [folio]="folio" heading="La Corona de Ceniza" headingId="page-heading">
      <p>Lead</p>
    </app-page-header>
  `,
})
class HostComponent {
  folio: string | undefined = '2 campañas';
}

describe('PageHeaderComponent', () => {
  it('renders the folio fact, a single h1 and projected lead content', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('h1')?.textContent?.trim()).toBe('La Corona de Ceniza');
    expect(element.querySelector('h1')?.id).toBe('page-heading');
    expect(element.textContent).toContain('2 campañas');
    expect(element.textContent).toContain('Lead');
  });

  it('omits the folio line when there is no fact to show', () => {
    const fixture = TestBed.createComponent(HostComponent);
    fixture.componentInstance.folio = undefined;
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelectorAll('p.folio')).toHaveLength(0);
  });
});
