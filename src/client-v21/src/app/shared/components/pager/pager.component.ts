import { Component, input, output } from '@angular/core';
import { MatPaginatorModule } from '@angular/material/paginator';

@Component({
  selector: 'app-pager',
  standalone: true,
  imports: [MatPaginatorModule],
  templateUrl: './pager.component.html',
  styleUrl: './pager.component.scss'
})
export class PagerComponent {
  totalCount = input.required<number>();
  pageSize = input.required<number>();
  pageNumber = input.required<number>();

  pageChanged = output<number>();

  handlePageChange(event: { pageIndex: number }): void {
    this.pageChanged.emit(event.pageIndex + 1);
  }
}
