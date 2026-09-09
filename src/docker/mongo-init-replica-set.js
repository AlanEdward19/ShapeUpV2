try {
  rs.status();
  print('Replica set already initiated.');
} catch (e) {
  rs.initiate({
    _id: 'rs0',
    members: [{ _id: 0, host: 'mongo:27017' }]
  });
  print('Replica set initiated.');
}
