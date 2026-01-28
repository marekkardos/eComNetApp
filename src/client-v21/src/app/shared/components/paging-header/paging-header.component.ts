import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'app-paging-header',
  standalone: true,
  templateUrl: './paging-header.component.html',
  styleUrl: './paging-header.component.scss'
})
export class PagingHeaderComponent {
  pageNumber = input.required<number>();
  pageSize = input.required<number>();
  totalCount = input.required<number>();

  startRange = computed(() => (this.pageNumber() - 1) * this.pageSize() + 1);

  endRange = computed(() => {
    const end = this.pageNumber() * this.pageSize();
    return end > this.totalCount() ? this.totalCount() : end;
  });

  hasResults = computed(() => this.totalCount() > 0);
}
