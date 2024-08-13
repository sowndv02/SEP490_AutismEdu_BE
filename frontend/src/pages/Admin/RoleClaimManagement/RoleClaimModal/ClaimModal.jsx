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

function ClaimModal() {
    const [open, setOpen] = useState(false);
    const handleOpen = () => setOpen(true);
    const handleClose = () => setOpen(false);

    const [claim, setClaim] = useState('');
    const [value, setValue] = useState("");
    const handleChange = (event) => {
        setClaim(event.target.value);
    };

    useEffect(() => {
        if (!open) {
            setClaim("");
            setValue("")
        }
    }, [open])
    return (
        <div>
            <Button variant="contained" startIcon={<AddIcon />} onClick={handleOpen}>Add Claim</Button>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-modal-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography id="modal-modal-title" variant="h6" component="h2">
                        Create new claim
                    </Typography>
                    <Box mt="20px">
                        <FormControl fullWidth size='small'>
                            <InputLabel id="type-claim">Type</InputLabel>
                            <Select
                                labelId="type-claim"
                                id="type-claim-select"
                                value={claim}
                                label="Type"
                                onChange={handleChange}
                            >
                                <MenuItem value={10}>Create</MenuItem>
                                <MenuItem value={20}>View</MenuItem>
                                <MenuItem value={30}>Edit</MenuItem>
                                <MenuItem value={30}>Delete</MenuItem>
                            </Select>
                        </FormControl>
                        <TextField size='small' id="outlined-basic" label="Claim value" variant="outlined"
                            value={value}
                            onChange={(e) => { setValue(e.target.value) }}
                            sx={{
                                width: "100%",
                                marginTop: "20px"
                            }} />
                        <Button variant='contained' sx={{ marginTop: "20px" }}>Add</Button>
                    </Box>
                </Box>
            </Modal>
        </div>
    );
}

export default ClaimModal
