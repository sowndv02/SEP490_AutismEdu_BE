import React, { useState } from 'react';
import BorderColorIcon from '@mui/icons-material/BorderColor';
import { Box, Divider, IconButton, Modal, Tooltip, Typography, Button } from '@mui/material';
import ReactQuill from 'react-quill';
import 'react-quill/dist/quill.snow.css';

function EditAboutMe({ text, setText }) {
    const [open, setOpen] = useState(false);
    const [editorContent, setEditorContent] = useState(text);

    const handleOpen = () => setOpen(true);
    const handleClose = () => {
        setOpen(false);
        setEditorContent(text);
    };

    const handleSave = () => {
        console.log('Saved content: ', editorContent);
        setText(editorContent);
        handleClose();
    };

    const toolbarOptions = [
        ['bold', 'italic', 'underline', 'strike'],
        ['blockquote', 'code-block'],
        ['link', 'image', 'video', 'formula'],
        [{ 'header': 1 }, { 'header': 2 }],
        [{ 'list': 'ordered' }, { 'list': 'bullet' }, { 'list': 'check' }],
        [{ 'script': 'sub' }, { 'script': 'super' }],
        [{ 'indent': '-1' }, { 'indent': '+1' }],
        [{ 'direction': 'rtl' }],
        [{ 'size': ['small', false, 'large', 'huge'] }],
        [{ 'header': [1, 2, 3, 4, 5, 6, false] }],
        [{ 'color': [] }, { 'background': [] }],
        [{ 'font': [] }],
        [{ 'align': [] }],
        ['clean']
    ];

    const style = {
        position: 'absolute',
        top: '50%',
        left: '50%',
        transform: 'translate(-50%, -50%)',
        width: 800,
        bgcolor: 'background.paper',
        boxShadow: 24,
        p: 4,
    };

    // Style for ReactQuill container and editor
    const quillContainerStyle = {
        height: '300px', // Set a fixed height
        overflowY: 'auto', // Enable vertical scrolling
        overflowX: 'hidden', // Disable horizontal scrolling
    };

    const quillEditorStyle = {
        height: '300px',
        overflowY: 'auto',
        overflowX: 'hidden', 
    };

    return (
        <>
            <Tooltip title="Chỉnh sửa" placement="top" arrow>
                <IconButton color="inherit" onClick={handleOpen}>
                    <BorderColorIcon />
                </IconButton>
            </Tooltip>
            <Modal
                open={open}
                onClose={handleClose}
                aria-labelledby="modal-x-title"
                aria-describedby="modal-modal-description"
            >
                <Box sx={style}>
                    <Typography textAlign={'center'} variant='h5' mb={2} id="modal-x-title">Chỉnh sửa giới thiệu</Typography>
                    <Divider />
                    <Box mt={2} style={quillContainerStyle}>
                        <ReactQuill
                            value={editorContent}
                            onChange={setEditorContent}
                            theme="snow"
                            modules={{
                                toolbar: toolbarOptions,
                            }}
                            style={quillEditorStyle} 
                        />
                    </Box>
                    <Box mt={3} display="flex" justifyContent="flex-end">
                        <Button variant="contained" color="inherit" onClick={handleClose} sx={{ mr: 2 }}>
                            Huỷ
                        </Button>
                        <Button variant="contained" color="primary" onClick={handleSave}>
                            Lưu
                        </Button>
                    </Box>
                </Box>
            </Modal>
        </>
    );
}

export default EditAboutMe;
