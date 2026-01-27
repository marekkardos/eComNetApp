export interface Pagination<T> {
  pageIndex: number;
  pageSize: number;
  count: number;
  data: T[];
}

export interface ShopParams {
  brandId: number;
  typeId: number;
  sort: string;
  pageNumber: number;
  pageSize: number;
  search: string;
}
