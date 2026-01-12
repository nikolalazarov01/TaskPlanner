export function createDataTransfer() {
  // JSDOM doesn't implement DataTransfer well enough for drag/drop
  return {
    data: {} as Record<string, string>,
    setData(type: string, val: string) {
      this.data[type] = val;
    },
    getData(type: string) {
      return this.data[type];
    },
    effectAllowed: 'move',
    dropEffect: 'move',
    setDragImage() {},
    files: [],
    items: [],
    types: [],
  };
}
