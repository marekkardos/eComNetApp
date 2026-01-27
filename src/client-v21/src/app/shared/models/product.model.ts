export interface Product {
  id: number;
  name: string;
  description: string;
  price: number;
  pictureUrl: string;
  productType: string;
  productBrand: string;
}

export interface Brand {
  id: number;
  name: string;
}

export interface ProductType {
  id: number;
  name: string;
}
