import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Modal from '@mui/material/Modal';
import { useEffect, useState } from 'react';
import AddIcon from '@mui/icons-material/Add';
import { FormControl, InputLabel, MenuItem, Select, TextField } from '@mui/material';
const style = {
    position: 'absolute',
    top: '50%',
    left: '50%',
    transform: 'translate(-50%, -50%)',
    width: 400,
    bgcolor: 'background.paper',
    boxShadow: 24,
    p: 4,
};

function RoleModal() {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const [role, setRole] = useState('');
    useEffect(() => {
        if (!open) {
            setRole("")
        }
    }, [open])
    return (
        <div>
            <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpen}>Add Role</Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Create new role
                    </Typography>
                    <Box mt="20px">
                        <TextField size='small' id="outlined-basic" label="Role" variant="outlined"
                            value={role}
                            onChange={(e) => { setRole(e.target.value) }}
                            sx={{
                                width: "100%"
                            }} />
                        <Button variant='contained' sx={{ marginTop: "20px" }}>Add</Button>
                    </Box>
                </Box>
            </Modal>
        </div>
    );
}

export default RoleModal
