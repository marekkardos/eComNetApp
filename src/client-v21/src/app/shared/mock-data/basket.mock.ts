import { BasketItem, BasketTotals } from '../models';
import { DeliveryMethod } from '../models';

export const MOCK_BASKET_ITEMS: BasketItem[] = [
  {
    id: 1,
    productName: 'Angular Purple Boots',
    price: 199.99,
    quantity: 2,
    pictureUrl: 'https://picsum.photos/seed/boot1/400/400',
    brand: 'Angular',
    type: 'Boots'
  },
  {
    id: 4,
    productName: 'TypeScript Navy Hat',
    price: 29.99,
    quantity: 1,
    pictureUrl: 'https://picsum.photos/seed/hat1/400/400',
    brand: 'TypeScript',
    type: 'Hats'
  },
  {
    id: 2,
    productName: 'React Blue Sneakers',
    price: 149.99,
    quantity: 1,
    pictureUrl: 'https://picsum.photos/seed/sneaker1/400/400',
    brand: 'React',
    type: 'Sneakers'
  }
];

export const MOCK_DELIVERY_METHODS: DeliveryMethod[] = [
  {
    id: 1,
    shortName: 'UPS1',
    deliveryTime: '1-2 Days',
    description: 'Fastest delivery time',
    price: 10
  },
  {
    id: 2,
    shortName: 'UPS2',
    deliveryTime: '2-5 Days',
    description: 'Get it within 5 days',
    price: 5
  },
  {
    id: 3,
    shortName: 'UPS3',
    deliveryTime: '5-10 Days',
    description: 'Slower but cheaper',
    price: 2
  },
  {
    id: 4,
    shortName: 'FREE',
    deliveryTime: '1-2 Weeks',
    description: 'Free! You get what you pay for',
    price: 0
  }
];

export function calculateBasketTotals(items: BasketItem[], shippingPrice: number = 0): BasketTotals {
  const subtotal = items.reduce((sum, item) => sum + (item.price * item.quantity), 0);
  return {
    shipping: shippingPrice,
    subtotal,
    total: subtotal + shippingPrice
  };
}
