import { Avatar, Box, Button, IconButton, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import RemoveRedEyeIcon from '@mui/icons-material/RemoveRedEye';
import DoneIcon from '@mui/icons-material/Done';
import BasicInformation from './BasicInformation';
import TutorCertificate from './TutorCertificate';
import TutorWorkExperience from './TutorWorkExperience';
function TutorRegistrationTable({ setPagination, pagination }) {

    return (
        <TableContainer component={Paper} sx={{ mt: "20px" }}>
            <Table>
                <TableHead>
                    <TableRow>
                        <TableCell>STT</TableCell>
                        <TableCell>Người dùng</TableCell>
                        <TableCell align='center'>Thông tin cơ bản</TableCell>
                        <TableCell align='center'>Bằng cấp / chứng chỉ</TableCell>
                        <TableCell align='center'>Kinh nghiệm làm việc</TableCell>
                        <TableCell>Hành động</TableCell>
                    </TableRow>
                </TableHead>
                <TableBody>
                    <TableRow>
                        <TableCell>1</TableCell>
                        <TableCell>
                            <Box sx={{ display: "flex", gap: 1 }}>
                                <Avatar alt="Remy Sharp" />
                                <Box>
                                    <Typography sx={{ fontWeight: "bold" }}>Đào Khải</Typography>
                                    <Typography sx={{ fontSize: "12px" }}>daoquangkhai2002@gmail.com</Typography>
                                </Box>
                            </Box>
                        </TableCell>
                        <TableCell align='center'>
                            <BasicInformation />
                        </TableCell>
                        <TableCell align='center'>
                            <TutorCertificate />
                        </TableCell>
                        <TableCell align='center'>
                            <TutorWorkExperience />
                        </TableCell>
                        <TableCell>
                            <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
                                <Button color='success' variant='contained'>Chấp nhận</Button>
                                <Button color='error' variant='contained'>Từ chối</Button>
                            </Box>
                        </TableCell>
                    </TableRow>
                </TableBody>
            </Table>
        </TableContainer >
    )
}

export default TutorRegistrationTable
