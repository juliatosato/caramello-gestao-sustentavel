export function dataInput(data = new Date()) {
  const local = new Date(data.getTime() - data.getTimezoneOffset() * 60000);
  return local.toISOString().slice(0, 10);
}

export function inicioPadrao() {
  const data = new Date();
  data.setDate(data.getDate() - 29);
  return dataInput(data);
}
