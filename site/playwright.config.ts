import { defineConfig } from '@playwright/test';
import 'dotenv/config';
export default defineConfig({testDir:'./tests',workers:1,use:{baseURL:'http://127.0.0.1:3100'},webServer:{command:'npm run setup:test && npx next start --hostname 127.0.0.1 --port 3100',url:'http://127.0.0.1:3100',env:{DATABASE_URL:'file:./test.db',NEXTAUTH_URL:'http://127.0.0.1:3100',UPLOAD_DIR:'uploads-test',BACKUP_DIR:'backups-test'},reuseExistingServer:false,timeout:120000}});
