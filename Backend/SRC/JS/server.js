// Codigo do servidor

import express from 'express';
import cors from 'cors';
import routes from '../Routes/routes.ts';
import helpers from '../Helper/helper.js';

const app = express();

app.use(cors());
app.use(express.json());
app.use(routes);

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Servidor rodando na porta ${PORT}`);
});